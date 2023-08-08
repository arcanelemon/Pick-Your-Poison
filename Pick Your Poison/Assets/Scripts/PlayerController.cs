using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Controls")]

    [SerializeField]
    private float exitHoldTime;


    [Header("Components")]

    [SerializeField]
    private InputActionAsset inputActions;

    [SerializeField]
    private Collider col;

    [SerializeField]
    private Rigidbody rb;

    [SerializeField]
    private Transform cameraContainer;

    [SerializeField]
    private Transform swayContainer;

    [SerializeField]
    private Transform muzzlePoint;

    [SerializeField]
    private HUD hud;


    [Header("Collisions")]

    [SerializeField]
    private float focusCheckDistance;

    [SerializeField]
    private float focusCheckRadius;

    [SerializeField]
    private float collisionThreshold;

    [SerializeField]
    private float collisionDrawOffset;

    [SerializeField]
    private LayerMask focusLayer;

    [SerializeField]
    private LayerMask groundLayer;


    [Header("Movement")]

    [SerializeField]
    private float baseSpeed;

    [SerializeField]
    private float lookSensitivity;

    [SerializeField]
    private float rotationSmoothing;

    [SerializeField]
    private float cameraTilt;

    [SerializeField]
    private float cameraTiltSpeed;

    [SerializeField]
    private float verticalBobAmount;

    [SerializeField]
    private float horizontalBobAmount;

    [SerializeField]
    private float bobFrequency;

    [SerializeField]
    private float acceleration;

    [SerializeField]
    private float friction;

    [SerializeField]
    [Range(0, 1)]
    private float horizontalMovementInfluence;

    [SerializeField]
    [Range(0, 1)]
    private float backwardMovementInfluence;


    [Header("Combat")]

    [SerializeField]
    private float baseHealth;

    [SerializeField]
    private float baseFireRate;

    [SerializeField]
    private float baseReloadTime;

    [SerializeField]
    private int baseAmmoCapacity;

    [SerializeField]
    private float baseVerticalRecoil;

    [SerializeField]
    private float baseHorizontalRecoil;

    [SerializeField]
    private float recoilReturnSpeed;

    [SerializeField]
    private float recoilSpeed;

    [SerializeField]
    private float recoilRestDelay;


    [Header("Projectile")]

    [SerializeField]
    private float baseProjectileDamage;

    [SerializeField]
    private int baseProjectileCount;

    [SerializeField]
    private float baseProjectileSpeed;

    [SerializeField]
    private float baseProjectileSpread;

    [SerializeField]
    private GameObject projectilePrefab;

    [SerializeField]
    private GameObject muzzleFlash;


    private InputActionMap inputMap;

    private float fireRate;

    private int ammoCapacity;

    private float reloadTime;

    private int projectileCount;

    private float projectileDamage;

    private float projectileSpeed;

    private float projectileSpread;


    private float d;

    private bool reloading;

    private bool firing;

    private bool recoilReturning;

    private bool recoiling;

    private bool canFire = true;

    private bool exitPressed;

    private int currAmmo;

    private float currSpeed;

    private float roll = 0;

    private float exitTime;

    private float recoilTime;

    private float recoilRestTime;

    private float horizontalRecoil;

    private float verticalRecoil;

    private Vector2 lookDelta;

    private Vector2 moveDelta;

    private Vector3 normal;

    private Vector2 recoilCurrent;

    private Vector2 recoilOrigin;

    private Vector2 recoilTarget;

    private Vector3 recoilOverride;

    private Pickup focus;

    #region Base

    // Start is called before the first frame update
    private void Awake()
    {
        currSpeed = baseSpeed;
        fireRate = baseFireRate;
        ammoCapacity = baseAmmoCapacity;
        currAmmo = ammoCapacity;
        reloadTime = baseReloadTime;
        projectileCount = baseProjectileCount;
        projectileDamage = baseProjectileDamage;
        projectileSpeed = baseProjectileSpeed;
        projectileSpread = baseProjectileSpread;
        horizontalRecoil = baseHorizontalRecoil;
        verticalRecoil = baseVerticalRecoil;

        inputMap = inputActions.FindActionMap("Player");

        var lookAction = inputMap.FindAction("Look");
        var moveAction = inputMap.FindAction("Move");
        var fireAction = inputMap.FindAction("Fire");
        var reloadAction = inputMap.FindAction("Reload");
        var pickupAction = inputMap.FindAction("Pickup");
        var exitAction = inputMap.FindAction("Exit");

        lookAction.performed += (context) => OnLookPerformed(context.ReadValue<Vector2>());
        moveAction.performed += (context) => OnMoveChanged(context.ReadValue<Vector2>());
        moveAction.canceled += (context) => OnMoveChanged(context.ReadValue<Vector2>());
        fireAction.performed += _ => OnFirePerformed();
        fireAction.canceled += _ => OnFireCanceled();
        reloadAction.performed += _ => OnReloadPerformed();
        pickupAction.performed += _ => OnPickupPerformed();
        exitAction.performed += _ => OnExitPerformed();
        exitAction.canceled += _ => OnExitCanceled();

        Enable();
    }

    private void Update()
    {
        if (exitPressed)
            IncrementExitTime();

        CheckCollisions();
        HandleLook();
        EvaluateRecoil();
    }

    private void LateUpdate()
    {
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        d = Time.deltaTime;

        HandleMove();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    #endregion Base

    #region Input

    private void OnLookPerformed(Vector2 input)
    {
        lookDelta = input * lookSensitivity;
    }

    private void OnMoveChanged(Vector2 input)
    {
        moveDelta = input;
    }

    private void OnFirePerformed()
    {
        if (!canFire || reloading)
            return;
        else if (currAmmo <= 0)
            HandleReload();
        else
            HandleFire();
    }

    private void OnFireCanceled()
    {
        firing = false;
    }

    private void OnReloadPerformed()
    {
        if (reloading || currAmmo == ammoCapacity)
            return;

        HandleReload();
    }

    private void OnPickupPerformed()
    {
        if (focus)
            HandlePickup();
    }

    private void OnExitPerformed()
    {
        exitPressed = true;
    }

    private void OnExitCanceled()
    {
        exitTime = 0;
        exitPressed = false;
        hud.clearExit();
    }

    private void Dispose()
    {
        Disable();

        inputMap.Dispose();
        StopAllCoroutines();
    }

    public void Enable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        inputMap.Enable();
    }

    public void Disable()
    {
        inputMap.Disable();
    }

    #endregion Input

    #region Actions

    private void HandleLook()
    {
        // roll
        float z = Mathf.LerpAngle(cameraContainer.localEulerAngles.z, roll, cameraTiltSpeed * d);
        Vector3 targetEuler = new Vector3(cameraContainer.localEulerAngles.x, cameraContainer.localEulerAngles.y, z);

        // lookDelta
        if (lookDelta != Vector2.zero)
        {
            // y rotation
            float pitch = cameraContainer.localEulerAngles.x;
            float localX = (pitch > 180) ? pitch - 360f : pitch;
            pitch = localX - lookDelta.y;
            pitch = Mathf.Clamp(pitch, -90, 90);

            // x rotation
            float yaw = rb.transform.eulerAngles.y + lookDelta.x;

            // --------------------------

            // pitch
            targetEuler.x = pitch;

            // yaw
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(0, yaw, 0), rotationSmoothing * d));
            lookDelta = Vector2.zero;
        }

        // apply to anchor
        cameraContainer.localRotation = Quaternion.Slerp(cameraContainer.localRotation, Quaternion.Euler(targetEuler), rotationSmoothing * d);
    }

    private void HandleMove()
    {

        // camera tilt
        roll = -cameraTilt * moveDelta.x;

        // camera bob
        float yBobAmount = 0;
        float xBobAmount = 0;
        float moveMultiplier = (new Vector2(rb.velocity.x, rb.velocity.z).magnitude / currSpeed);

        yBobAmount = verticalBobAmount * moveMultiplier / 4 *
            Mathf.Sin(bobFrequency * Time.time);
        xBobAmount = horizontalBobAmount * moveMultiplier / 4 *
            Mathf.Sin(bobFrequency / 2 * Time.time);

        Vector3 targetPosition = swayContainer.localPosition;
        targetPosition.y = yBobAmount; //Mathf.Lerp(targetPosition.y, yBobAmount, bobFrequency * d);
        targetPosition.x = xBobAmount; //Mathf.Lerp(targetPosition.x, xBobAmount, bobFrequency * d);

        Debug.Log(yBobAmount);
        swayContainer.localPosition = targetPosition;

        // move rigidbody
        if (moveDelta == Vector2.zero)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, friction * 10 * d);
            return;
        }

        float horizontalInput = moveDelta.x * horizontalMovementInfluence;
        float verticalInput = moveDelta.y * (moveDelta.y < -0.1f ? backwardMovementInfluence : 1f);
        Vector3 direction = verticalInput * transform.forward + horizontalInput * transform.right;

        direction = Vector3.ProjectOnPlane(direction.normalized, normal); // project onto normal
        Vector3 velocity = currSpeed * direction;
        rb.velocity = Vector3.Lerp(rb.velocity, velocity, acceleration * 10 * d);
    }

    private void HandleFire()
    {
        firing = true;
        currAmmo -= 1;
        GenerateRecoil();
        StartCoroutine(YieldFireRateCoroutine());
    }

    private void HandleReload()
    {
        reloading = true;
        hud.showReload();
        StartCoroutine(YieldReloadCoroutine());
    }

    private void HandlePickup()
    {
        if (focus)
        {
            Destroy(focus.gameObject);
        }
    }

    void EvaluateRecoil()
    {
        if (!recoiling) return;

        if (Mathf.Approximately(Vector2.Distance(recoilCurrent, recoilReturning ? recoilOrigin : recoilTarget), 0))
        {
            // recoil done
            if (recoilReturning)
            {
                StopRecoil();
            }

            // recoil resting
            else
            {
                recoilRestTime += d;
                if (recoilRestTime >= recoilRestDelay)
                {
                    // set returning
                    recoilTime = 0;
                    recoilReturning = true;
                    recoilOrigin = Vector2.zero;
                }
            }

            return;
        }

        // return recoil
        if (recoilReturning)
            recoilCurrent = Vector2.Lerp(recoilTarget, recoilOrigin, recoilTime / recoilReturnSpeed);

        // increment recoil
        else
        {
                recoilCurrent = Vector2.Lerp(recoilOrigin, recoilTarget, recoilTime / recoilSpeed);
        }

        recoilTime += d;
    }

    void ApplyRecoilOverride()
    {
        if (!recoiling)
            return;

        // clamp override
        float pitch = (cameraContainer.localEulerAngles.x > 180) ? cameraContainer.localEulerAngles.x - 360f : cameraContainer.localEulerAngles.x;
        pitch = Mathf.Clamp(pitch + recoilCurrent.y, -90, 90);

        float yaw = cameraContainer.localEulerAngles.y + recoilCurrent.x;
        float roll = cameraContainer.localEulerAngles.z;

        // apply override
        Vector3 targetRotation = new Vector3(pitch, yaw, roll);
        cameraContainer.localEulerAngles = targetRotation - recoilOverride;
        recoilOverride = new Vector3(recoilCurrent.y, recoilCurrent.x, 0);
    }

    public void GenerateRecoil()
    {

        float x;
        float y;

        // range
        x = Random.Range(-horizontalRecoil, horizontalRecoil);
        y = (recoilReturning ? recoilCurrent.y : recoilTarget.y) - verticalRecoil;

        recoilTarget = new Vector2(x, y);
        Vector3 AB = recoilTarget - recoilOrigin;
        Vector3 AV = recoilCurrent - recoilOrigin;
        float recoilProgress = Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
        recoilTime = recoilSpeed * recoilProgress;
        recoilOrigin = new Vector2(recoilCurrent.x, 0);

        recoiling = true;
        recoilReturning = false;
        recoilRestTime = 0;
    }

    public void StopRecoil()
    {
        recoilTarget = Vector2.zero;
        recoilCurrent = Vector2.zero;
        recoilOrigin = Vector2.zero;
        recoilOverride = Vector3.zero;

        recoilTime = 0;
        recoiling = false;
        recoilReturning = false;
    }

    #endregion

    #region Utility

    private void IncrementExitTime()
    {
        exitTime += d;
        hud.updateExit(exitTime / exitHoldTime);

        if (exitTime >= exitHoldTime)
            GameManager.Instance.Quit();
    }

    private void CheckCollisions()
    {
        // ground check
        float radius = collisionDrawOffset;
        float distance = 2 * collisionThreshold;
        RaycastHit hit;
        Vector3 origin = transform.position + (radius + collisionThreshold) * Vector3.up;
        if (Physics.SphereCast(origin, radius, Vector3.down, out hit, distance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 contactPoint = origin + (Vector3.down * distance);
            Vector3 contactDirection = hit.point - origin;
            float contactOffset = contactDirection.magnitude + distance;

            RaycastHit spherePoint;
            if (Physics.Raycast(contactPoint, contactDirection, out spherePoint, contactOffset, groundLayer))
            {
                normal = spherePoint.normal;
            }
        }

        // check focus
        Collider[] focusCols = Physics.OverlapCapsule(cameraContainer.position, cameraContainer.position + (cameraContainer.forward * focusCheckDistance), focusCheckRadius, focusLayer, QueryTriggerInteraction.Collide);

        if (focusCols.Length > 0)
            focus = focusCols[0].GetComponent<Pickup>();
        else
            focus = null;

        if (focus)
        {
            hud.updateFocus(focus.Description);
        } else
        {
            hud.clearFocus();
        }
    }

    private void SpawnProjectiles()
    {
        Instantiate(muzzleFlash, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);

        foreach (int _ in Enumerable.Range(1, projectileCount))
        {
            Vector3 angle = projectileSpread * Random.insideUnitSphere + cameraContainer.forward;
            Projectile projectile = Instantiate(projectilePrefab, cameraContainer.position + cameraContainer.transform.forward, Quaternion.LookRotation(angle), null).GetComponent<Projectile>();
            projectile.Initialize(projectileDamage, projectileSpeed);
        }
    }

    #endregion Utility

    #region Coroutine

    private IEnumerator YieldFireRateCoroutine()
    {
        canFire = false;
        hud.showFire();
        SpawnProjectiles();
        yield return new WaitForSeconds(fireRate);
        canFire = true;

        if (firing)
            HandleFire();
    }

    private IEnumerator YieldReloadCoroutine()
    {
        float delta = 0;
        while (reloading)
        {
            yield return new WaitForFixedUpdate();

            delta += d;
            hud.updateReload(delta / reloadTime);
            if (delta >= reloadTime)
            {
                reloading = false;
                currAmmo = ammoCapacity;
            }
        }

        hud.hideReload();
    }

    #endregion Coroutine
}

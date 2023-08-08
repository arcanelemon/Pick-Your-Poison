using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI currencyCounter;

    [SerializeField]
    private GameObject reloadSpinner;

    [SerializeField]
    private Image reloadSpinFill;

    [SerializeField]
    private Animator exitAnimator;

    [SerializeField]
    private Image exitIconFill;

    [SerializeField]
    private GameObject exitText;

    [SerializeField]
    private TextMeshProUGUI pickupText;

    [SerializeField]
    private Animator fireVignetteAnimator;

    [SerializeField]
    private GameObject damagedVignette;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void updateCurrencyCounter(int amount)
    {
        currencyCounter.text = amount.ToString();
    }

    public void updateReload(float progress)
    {
        reloadSpinFill.fillAmount = progress;
    }

    public void showReload()
    {
        reloadSpinner.SetActive(true);
    }

    public void hideReload()
    {
        reloadSpinFill.fillAmount = 0;
        reloadSpinner.SetActive(false);
    }

    public void updateFocus(string focusName)
    {
        pickupText.text = focusName;
    }

    public void clearFocus()
    {
        pickupText.text = "";
    }

    public void showFire()
    {
        fireVignetteAnimator.Play("Play");
    }

    public void showDamage()
    {

    }

    public void updateExit(float progress)
    {
        exitAnimator.Play("Active");
        exitIconFill.fillAmount = progress;
        exitText.SetActive(false);
    }

    public void clearExit()
    {
        exitAnimator.Play("Idle");
        exitIconFill.fillAmount = 0;
        exitText.SetActive(true);
    }
}

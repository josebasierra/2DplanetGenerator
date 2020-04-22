using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControllerPG : MonoBehaviour 
{

    public PlanetGenerator planetGenerator;

    public Slider radiusSlider;
    public Slider caveDensitySlider;
    public Slider deformSlider;
    public Slider deformFreqSlider;

    public InputField seedField;
    public Text seedText;


    private void Start()
    {
        UpdateCanvas();
    }


    private void UpdateCanvas()
    {
        radiusSlider.value = planetGenerator.GetRadiusRatio();
        caveDensitySlider.value = planetGenerator.GetCaveDensityRatio();
        deformSlider.value = planetGenerator.GetDeformationRatio();
        deformFreqSlider.value = planetGenerator.GetDeformationFreqRatio();
        seedText.text = "Seed: " + planetGenerator.seed;
    }


    public void SetRandomSeed() 
    {
        planetGenerator.SetRandomSeed();
        planetGenerator.UpdatePlanetDisplay();
        UpdateCanvas();
    }


    public void SetSeed()
    {
        int seed = int.Parse(seedField.text);
        if (seed <= 0) seed = 1;
        planetGenerator.SetSeed((uint)seed);
        UpdateCanvas();
    }


    public void UpdatePlanetRadius()
    {
        planetGenerator.SetRadiusRatio(radiusSlider.value);
        planetGenerator.UpdatePlanetDisplay();
    }

    
    public void UpdateCaveDensity() 
    {
        planetGenerator.SetCaveDensityRatio(caveDensitySlider.value);
        planetGenerator.UpdatePlanetDisplay();
    }


    public void UpdateDeformation()
    {
        planetGenerator.SetDeformationRatio(deformSlider.value);
        planetGenerator.UpdatePlanetDisplay();
    }


    public void UpdateDeformationFreq()
    {
        planetGenerator.SetDeformationFreqRatio(deformFreqSlider.value);
        planetGenerator.UpdatePlanetDisplay();
    }


    public void CloseApp()
    {
        Application.Quit();
    }

}

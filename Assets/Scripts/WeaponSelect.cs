using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSelect : MonoBehaviour
{

    public GameObject rifle;
    public GameObject pistol;

    public static bool isRifle;
    public static bool isPistol;

  
    void Start()
    {
        rifle.SetActive(false);
        pistol.SetActive(false);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            isRifle = true;
            isPistol = false;
           rifle.SetActive(true);
           pistol.SetActive(false);
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            isRifle = false;
            isPistol = true;
           rifle.SetActive(false);
           pistol.SetActive(true);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            isRifle = false;
            isPistol = false;
            rifle.SetActive(false);
            pistol.SetActive(false);
        }
    }
}

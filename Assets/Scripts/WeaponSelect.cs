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
    public static bool isEmpty = true;



  
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
            isEmpty = false;
           rifle.SetActive(true);
           pistol.SetActive(false);
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            isRifle = false;
            isPistol = true;
            isEmpty = false;
           rifle.SetActive(false);
           pistol.SetActive(true);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            isRifle = false;
            isPistol = false;
            isEmpty = true;
            rifle.SetActive(false);
            pistol.SetActive(false);
        }
    }
}

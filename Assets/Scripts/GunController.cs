using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public Transform weaponTransform;
    public Vector3 recoil = new Vector3(0f, 0f, -2f);
    public float returnSpeed = 5f;
    public float snappiness = 10f;


    private Vector3 currentRecoil; 
    private Vector3 targetRecoil;
    private Vector3 initialPosition;
    private void Start()
    {
        initialPosition = weaponTransform.localPosition;
    }

    void Update()
    {
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, Time.deltaTime * returnSpeed);
        currentRecoil = Vector3.Lerp(currentRecoil, targetRecoil, Time.deltaTime * snappiness);
        weaponTransform.localPosition = initialPosition + currentRecoil;

        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }
    }

    public void Fire()
    {
        targetRecoil += recoil;

        if (WeaponSelect.isRifle)
        {
            RuntimeManager.PlayOneShot(ReverbManager.RevInstance.rifleSfx);
        }
        if (WeaponSelect.isPistol)
        {
            RuntimeManager.PlayOneShot(ReverbManager.RevInstance.pistolSfx);
        } 
    }
}

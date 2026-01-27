using FMOD.Studio;
using FMODUnity;
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

    // SİLDİK: private FMOD.Studio.EventInstance musicInstance; 
    // Artık sesi elimizde tutmamıza gerek yok, hoparlörün kendisi tutuyor.

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

        if (Input.GetKeyDown(KeyCode.Alpha2))

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



        // ETKİLEŞİM KISMI
        if (Input.GetKeyDown(KeyCode.F))
        {
            Vector3 origin = transform.position + (Vector3.up);

            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, 5f))
            {
                Debug.DrawLine(origin, hit.point, Color.red, 2f);

                // Eğer vurduğumuz objede FMOD Emitter varsa onu kullanalım
                if (hit.collider.tag == "speaker")
                {
                    // Objeye "Senin üzerindeki Emitter bileşeni ver" diyoruz
                    var emitter = hit.collider.GetComponent<StudioEventEmitter>();
                    if (emitter != null)
                    {
                        ToggleMusic(emitter);
                    }
                }
            }
        }
    }

    // Yeni Fonksiyon: Emitter'ı direkt kontrol eder
    void ToggleMusic(StudioEventEmitter emitter)
    {
        if (emitter.IsPlaying())
        {
            // Resimde "Allow Fadeout" seçili olduğu için, Stop dediğinde
            // FMOD otomatik olarak sesi yumuşatarak kapatacak.
            emitter.Stop();
            Debug.Log("Müzik Durduruldu (Emitter üzerinden).");
        }
        else
        {
            emitter.Play();
            Debug.Log("Müzik Başlatıldı (Emitter üzerinden).");
        }
    }
}
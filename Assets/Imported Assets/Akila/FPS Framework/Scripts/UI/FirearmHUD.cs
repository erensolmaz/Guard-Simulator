using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Reflection;

namespace Akila.FPSFramework
{
    [AddComponentMenu("Akila/FPS Framework/Weapons/Firearm HUD")]
    public class FirearmHUD : MonoBehaviour
    {
        [Header("Text")]
        public TextMeshProUGUI firearmNameText;
        public TextMeshProUGUI ammoTypeNameText;
        public TextMeshProUGUI remainingAmmoText;
        public TextMeshProUGUI remainingAmmoTypeText;
        public GameObject outOfAmmoAlert;
        public GameObject lowAmmoAlert;

        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color alertColor = Color.red;

        public Firearm firearm { get; set; }

        private void Update()
        {
            if (!firearm)
            {
                return;
            }

            gameObject.SetActive(firearm.isHudActive);

            // Bot silahı kontrolü - bot silahları için HUD alert'lerini devre dışı bırak
            // Reflection kullanarak GuardSimulator.Character.BotFirearmController'ı kontrol et
            bool isBotWeapon = false;
            try
            {
                var botControllerType = System.Type.GetType("GuardSimulator.Character.BotFirearmController, Assembly-CSharp");
                if (botControllerType != null)
                {
                    var botController = firearm.GetComponent(botControllerType);
                    if (botController != null)
                    {
                        var isBotWeaponMethod = botControllerType.GetMethod("IsBotWeapon", BindingFlags.Public | BindingFlags.Instance);
                        if (isBotWeaponMethod != null)
                        {
                            bool result = (bool)isBotWeaponMethod.Invoke(botController, null);
                            if (result)
                            {
                                isBotWeapon = true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Reflection başarısız olursa devam et (normal silah olarak kabul et)
            }

            firearmNameText.SetText(firearm.Name);
            ammoTypeNameText.SetText(firearm.ammoProfile.identifier.displayName);
            remainingAmmoText.SetText(firearm.remainingAmmoCount.ToString());
            remainingAmmoTypeText.SetText(firearm.remainingAmmoTypeCount.ToString());

            // Bot silahıysa outOfAmmoAlert'i devre dışı bırak
            if (!isBotWeapon)
            {
                outOfAmmoAlert.SetActive(firearm.remainingAmmoCount <= 0);
                lowAmmoAlert.SetActive(firearm.remainingAmmoCount <= firearm.preset.magazineCapacity / 3 && firearm.remainingAmmoCount > 0);
            }
            else
            {
                // Bot silahı için alert'leri devre dışı bırak
                outOfAmmoAlert.SetActive(false);
                lowAmmoAlert.SetActive(false);
            }

            remainingAmmoText.color = firearm.remainingAmmoCount <= firearm.preset.magazineCapacity / 3 ? alertColor : normalColor;
            remainingAmmoTypeText.color = firearm.remainingAmmoTypeCount <= 0 ? alertColor : normalColor;
        }

        private void LateUpdate()
        {
            if(firearm == null)
            {
                Destroy(gameObject);
            }
        }
    }
}
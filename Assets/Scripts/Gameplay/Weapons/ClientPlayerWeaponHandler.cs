﻿using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Weapons
{
    public class ClientPlayerWeaponHandler : NetworkBehaviour
    {
        [SerializeField] private Weapon activeWeapon;
        [SerializeField] private List<Weapon> availableWeapons;

        [SyncVar(hook = nameof(OnWeaponChanged))] 
        public int activeWeaponIndexSynced = 0;

        private float _weaponCooldownTime;
        private Camera _camera;

        public override void OnStartLocalPlayer() 
            => enabled = !isServer;

        #region InputAction Events

        public void OnFire(InputAction.CallbackContext context)
        {
            if (!hasAuthority || !isLocalPlayer || isServer)
                return;

            if (!context.performed)
                return;

            if (!CanShoot())
                return;

            var targetPoint = GetTargetShootPoint();
            CmdFire(targetPoint);
        }

        public void OnWeaponSwitch(InputAction.CallbackContext context)
        {
            if (!hasAuthority || !isLocalPlayer || isServer)
                return;

            if (!context.performed)
                return;

            if (!CanSwitchWeapon())
                return;

            var direction = context.ReadValue<Vector2>().y;

            if (direction == 0f)
                return; // I have no idea why this event is triggered always twice

            var weaponSwitchDirection = Mathf.Sign(direction);
            if (weaponSwitchDirection == 1f)
            {
                // Next weapon
                var requestedWeaponIndex = activeWeaponIndexSynced + 1;

                if (requestedWeaponIndex > availableWeapons.Count - 1)
                    CmdSwitchWeapon(0); // We had last weapon so we switch to the first on the list
                else 
                    CmdSwitchWeapon(requestedWeaponIndex);
            }
            else
            {
                // Previous weapon
                var requestedWeaponIndex = activeWeaponIndexSynced - 1;

                if (requestedWeaponIndex < 0)
                    CmdSwitchWeapon(availableWeapons.Count - 1); // Last weapon
                else
                    CmdSwitchWeapon(requestedWeaponIndex);
            }
        }

        #endregion


        [ClientCallback]
        private void Awake() 
            => _camera = Camera.main;

        #region Server

        [Command]
        public void CmdFire(Vector3 targetPoint)
        {
            if (!CanShoot())
                return;
            
            _weaponCooldownTime = Time.time + activeWeapon.weaponCooldown;

            activeWeapon.Fire(targetPoint);
        }

        [Command]
        public void CmdSwitchWeapon(int weaponIndex)
        {
            if (!CanSwitchWeapon())
                return;

            activeWeaponIndexSynced = weaponIndex;
        }

        #endregion

        #region Client

        private void OnWeaponChanged(int oldIndex, int newIndex)
        {
            activeWeapon.weaponRenderer.enabled = false;

            activeWeapon = availableWeapons[newIndex];

            activeWeapon.weaponRenderer.enabled = true;
        }

        #endregion

        private bool CanShoot()
            => activeWeapon != null &&
               activeWeapon.weaponAmmo > 0 &&
               Time.time > _weaponCooldownTime;

        private Vector3 GetTargetShootPoint()
        {
            var screenCenterRay = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            Vector3 targetPoint;
            if (Physics.Raycast(screenCenterRay, out var hit))
                targetPoint = hit.point;
            else
                targetPoint = screenCenterRay.GetPoint(1000);

            return targetPoint;
        }

        private bool CanSwitchWeapon() 
            => availableWeapons.Count > 1;
    }
}

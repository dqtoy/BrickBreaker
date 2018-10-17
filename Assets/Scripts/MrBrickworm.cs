﻿#pragma warning disable 0414

using Assets.Classes;
using Assets.Interfaces;
using Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts {
    public class MrBrickworm : Boss {
        [Header("1. Stats")]
        [SerializeField] int HealthPointsBase = 100;
        [SerializeField] int HealthPointsCurrent;
        [SerializeField] int DamageByBall = 1;
        [SerializeField] int DamageByFireball = 5;
        [SerializeField] float DelayHBarDisable = 0.5f;
        [Header("3. Chaches")]
        [SerializeField] BossAttackManager attackManager;
        [SerializeField] AudioClip SFXArriveSound;
        [SerializeField] AudioClip SFXSpellSound;
        [SerializeField] AudioClip SFXOpeningDoors;
        [SerializeField] SpriteRenderer opennedDoors;
        [SerializeField] ParticleSystem PSPlatform;
        [SerializeField] ParticleSystem PSOpennedDoors;
        [SerializeField] Animator MrBAnimator;
        [SerializeField] ProgressBar HealthBar;
        [Header("2. Movement props")]
        [SerializeField] GameObject PositionHandler;
        [SerializeField] MrBWaypoints Waypoints;
        [SerializeField] GameObject startPosition;
        [SerializeField] float RestingPeriod = 3f;
        [SerializeField] float MovementSpeed = 0.1f;

        //cached vars
        bool isMoving = false;
        [SerializeField] public bool arrived = false;
        [SerializeField] bool attackPhase = false;
        [SerializeField] Transform targetPosition = null;

        public override void Dying() {
            attackPhase = true;
            isMoving = false;
            MrBAnimator.SetTrigger(triggers.DYING);
            StartCoroutine(OnDying());
        }

        IEnumerator OnDying() {
            yield return new WaitForSeconds(5f);
            print("OnDieing completed");
            OnDeath();
        }

        new private void Start() {
            base.Start();
            GetTargetPosition();
            HealthPointsCurrent = HealthPointsBase;
            MrBAnimator = GameObject.Find(gameobjects.MRBRICKWORM).GetComponent<Animator>();
            HealthBar = GameObject.Find(gameobjects.MRBHEALTHBAR).GetComponent<ProgressBar>();
        }

        private void Update() {
            ResolveMoving();
        }

        private void ResolveMoving() {
            MrBAnimator.SetFloat(triggers.LEGWIGGLESPEED, movingAnimationSpeed);
            if (!ArrivedAtPoint() && arrived && !attackPhase) {
                MrBAnimator.SetBool(triggers.MOVELEGS, true);
                //print("should be moving, difference between current and target is: x: " + (PositionHandler.transform.position.x - targetPosition.position.x) + " y: " + (PositionHandler.transform.position.y - targetPosition.position.y));
                if(PositionHandler) PositionHandler.transform.position = Vector2.MoveTowards(PositionHandler.transform.position, targetPosition.position, MovementSpeed * Time.deltaTime);
            } else if(ArrivedAtPoint() && !attackPhase && arrived) {
                //print("MrBrickworm/ResolveMoving: Reached targetPosition");
                MrBAnimator.SetBool(triggers.MOVELEGS, false);
                StartCoroutine(AttackPhase());
            }
        }

        private bool ArrivedAtPoint() {
            if (PositionHandler) {
                if ((PositionHandler.transform.position.x - targetPosition.position.x) <= 0.01f &&
                        +(PositionHandler.transform.position.y - targetPosition.position.y) <= 0.01f)
                    return true; 
            } else print("MrBrickworm/ArrivedAtPoint: PositionHandler not found");
            return false;
        }

        private void GetTargetPosition() {
            targetPosition = Waypoints.GetWaypoints()
                [UnityEngine.Random.Range(0, Waypoints.GetWaypoints().Count())].transform;
        }

        IEnumerator AttackPhase() {
            attackPhase = true;
            isMoving = false;
            if (attackManager) attackManager.performAttack();
            else print("MrBrickworm/AttackPhase: missing attackManager");
            yield return new WaitForSeconds(RestingPeriod);
            GetTargetPosition();
            attackPhase = false;
            isMoving = true;
        }

        private void PerformAttack() {
            //MrBAnimator.SetTrigger("AttackLeft");
            MrBAnimator.SetTrigger("AttackRight");
        }

        public override void OnArrival() {
            //print("MrBrickworm: OnArrival: test OK");
            opennedDoors.enabled = true;
            SFXPlayer.PlayClipOnce(SFXOpeningDoors);
            PSOpennedDoors.Play();
            StartCoroutine(Arrive());
        }

        IEnumerator Arrive() {
            yield return new WaitForSeconds(0.5f);
            SFXPlayer.PlayClipOnce(SFXSpellSound);
            MrBAnimator.SetTrigger(triggers.START);
            yield return new WaitForSeconds(2f);
            HealthBar.EnableVisuals();
            PlayGrowl();
            yield return new WaitForSeconds(1.5f);
            arrived = true;
        }

        public void PlayGrowl() {
            SFXPlayer.PlayClipOnce(SFXArriveSound);
        }

        public void startPlatformPS() {
            PSPlatform.Play();
        }

        public override void OnCollisionEnter2D(Collision2D collision) {
            String collisionTag = collision.gameObject.tag;
            MrBAnimator.SetTrigger(triggers.HIT);
            HealthChange(collisionTag);
        }

        private void HealthChange(String collisionTag) {
            float change = 0f;
            print("MrBrickworm/HealthChange: collisionTag: " + collisionTag);
            if (collisionTag == tags.BALL) change = -DamageByBall;
            if (collisionTag == tags.FIREBALL) change = -DamageByFireball;
            ChangeHPValue((int)change);
            HealthBar.UpdateBar(healthChangeInPorcent(change));
            if (HealthPointsCurrent <= 0) Dying();
        }

        private void ChangeHPValue(int change) {
            HealthPointsCurrent += change;
        }

        private float healthChangeInPorcent(float damage) {
            //print("MrBrickworm/healthChangeInProcent: " + damage / (HealthPointsBase / 100));
            return (damage/(HealthPointsBase/100));
        }

        public override void OnDeath() {
            print("Boss \"died\"");
            StartCoroutine(AfterDeathSequence());
        }

        private IEnumerator AfterDeathSequence() {
            yield return new WaitForSeconds(DelayHBarDisable);
            HealthBar.DisableVisuals();
            SceneLoader SL = FindObjectOfType<SceneLoader>();
            if (SL) SL.LoadScene();
            else print("MrBrickworm/AterDeathSequence: no SceneLoader found");
        }

        public override void StartEncounter() {
            OnArrival();
        }

        public override SoundSystem.PlayListID GetPlayListID() {
            return SoundSystem.PlayListID.Boss;
        }
    }
}

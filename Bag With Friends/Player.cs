﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using RootMotion.FinalIK;

namespace Bag_With_Friends
{
    public class Player
    {
        Multiplayer manager;

        public string name;
        public long id;
        public string scene;
        public bool host;

        public GameObject body;
        public GameObject handL;
        public GameObject handR;
        public GameObject footL;
        public GameObject footR;
        public GameObject footLBend;
        public GameObject footRBend;

        public GameObject player;

        public Vector3 bodyPosition;
        public Vector3 handLPosition;
        public Vector3 handRPosition;
        public Vector3 footLPosition;
        public Vector3 footRPosition;
        public Vector3 footLBendPosition;
        public Vector3 footRBendPosition;

        public Quaternion bodyRotation;
        public Quaternion handLRotation;
        public Quaternion handRRotation;
        public Quaternion footLRotation;
        public Quaternion footRRotation;

        public ArmIK handLIK;
        public ArmIK handRIK;
        public LimbIK footLIK;
        public LimbIK footRIK;

        public AnimationCurve stretchCurve;

        public Player(string name, long id, string scene, bool host, Multiplayer manager)
        {
            this.name = name;
            this.id = id;
            this.scene = scene;
            this.host = host;
            this.manager = manager;
        }

        public void ChangeScene(string newScene)
        {
            manager.LoggerInstance.Msg("player scene " + newScene + ", my scene " + SceneManager.GetActiveScene().name);
            scene = newScene;
            UpdateVisual(SceneManager.GetActiveScene().name);
        }

        public void UpdateVisual(string meScene)
        {
            if (scene != meScene)
            {
                if (body != null)
                {
                    GameObject.Destroy(body);
                }

                if (handL != null)
                {
                    GameObject.Destroy(handL);
                    GameObject.Destroy(handR);
                    GameObject.Destroy(footL);
                    GameObject.Destroy(footR);
                    GameObject.Destroy(footLBend);
                    GameObject.Destroy(footRBend);
                }

                if (player != null)
                {
                    GameObject.Destroy(player);
                }
            }

            if (scene == meScene && meScene != "Cabin")
            {
                body = new GameObject(name + " Body");
                /*body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = name + " Body";
                body.transform.localScale = Vector3.one / 4f;
                GameObject.Destroy(body.GetComponent<Collider>());
                body.transform.SetParent(manager.playerContainer.transform);*/

                player = manager.shadowPrefabs[0];
                manager.shadowPrefabs.RemoveAt(0);
                player.name = name + " Body";
                player.transform.SetParent(manager.playerContainer.transform);

                SkinnedMeshRenderer[] skinnedMeshes = Multiplayer.GetAllSkinnedMeshRenderersInChildren(player);
                for (int i = 0; i < skinnedMeshes.Length; i++)
                {
                    SkinnedMeshRenderer skin = skinnedMeshes[i];
                    skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    skin.material.renderQueue = 1000;
                    skin.material.color = new Color(1, 1, 1, 1);
                    skin.material.shaderKeywords = new string[0] { };

                    manager.LoggerInstance.Msg(skin.name);
                }

                handL = new GameObject(name + " HandL");
                /*handL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handL.name = name + " HandL";
                handL.transform.localScale = Vector3.one / 4f;
                GameObject.Destroy(handL.GetComponent<Collider>());*/
                handL.transform.SetParent(manager.playerContainer.transform);

                handR = new GameObject(name + " HandR");
                /*handR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handR.name = name + " HandR";
                handR.transform.localScale = Vector3.one / 4f;
                GameObject.Destroy(handR.GetComponent<Collider>());*/
                handR.transform.SetParent(manager.playerContainer.transform);

                footL = new GameObject(name + " FootL");
                /*footL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                footL.name = name + " FootL";
                footL.transform.localScale = Vector3.one / 4f;
                GameObject.Destroy(footL.GetComponent<Collider>());*/
                footL.transform.SetParent(manager.playerContainer.transform);

                footR = new GameObject(name + " FootR");
                /*footR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                footR.name = name + " FootR";
                footR.transform.localScale = Vector3.one / 4f;
                GameObject.Destroy(footR.GetComponent<Collider>());*/
                footR.transform.SetParent(manager.playerContainer.transform);

                footLBend = new GameObject(name + " FootL Bend");
                /*footLBend = GameObject.CreatePrimitive(PrimitiveType.Cube);
                footLBend.name = name + " FootL Bend";
                footLBend.transform.localScale = Vector3.one / 4f;
                GameObject.Destroy(footLBend.GetComponent<Collider>());*/
                footLBend.transform.SetParent(manager.playerContainer.transform);

                footRBend = new GameObject(name + " FootR Bend");
                /*footRBend = GameObject.CreatePrimitive(PrimitiveType.Cube);
                footRBend.name = name + " FootR Bend";
                footRBend.transform.localScale = Vector3.one / 4f;
                GameObject.Destroy(footRBend.GetComponent<Collider>());*/
                footRBend.transform.SetParent(manager.playerContainer.transform);

                handLIK = player.transform.GetChild(5).GetComponent<ArmIK>();
                handRIK = player.transform.GetChild(6).GetComponent<ArmIK>();
                footLIK = player.transform.GetChild(3).GetComponent<LimbIK>();
                footRIK = player.transform.GetChild(4).GetComponent<LimbIK>();

                handLIK.solver.arm.target = handL.transform;
                handRIK.solver.arm.target = handR.transform;
                //handLIK.solver.arm.armLengthMlp = 1.4f;
                //handRIK.solver.arm.armLengthMlp = 1.4f;
                stretchCurve = new AnimationCurve();
                stretchCurve.AddKey(1, 1);
                stretchCurve.AddKey(1.25f, 1.25f);
                //handLIK.solver.arm.stretchCurve = stretchCurve;
                //handRIK.solver.arm.stretchCurve = stretchCurve;

                footLIK.solver.target = footL.transform;
                footRIK.solver.target = footR.transform;
                footLIK.solver.bendGoal = footLBend.transform;
                footRIK.solver.bendGoal = footRBend.transform;
            }
        }

        public void UpdatePosition(Vector3 bodyPosition, Vector3 handLPosition, Vector3 handRPosition, Vector3 footLPosition, Vector3 footRPosition, Vector3 footLBendPosition, Vector3 footRBendPosition, Quaternion bodyRotation, Quaternion handLRotation, Quaternion handRRotation, Quaternion footLRotation, Quaternion footRRotation)
        {
            if (body != null)
            {
                this.bodyPosition = bodyPosition;
                this.handLPosition = handLPosition;
                this.handRPosition = handRPosition;
                this.footLPosition = footLPosition;
                this.footRPosition = footRPosition;
                this.footLBendPosition = footLBendPosition;
                this.footRBendPosition = footRBendPosition;

                this.bodyRotation = bodyRotation;
                this.handLRotation = handLRotation;
                this.handRRotation = handRRotation;
                this.footLRotation = footLRotation;
                this.footRRotation = footRRotation;
            }
        }

        public void Yeet()
        {
            if (body != null)
            {
                GameObject.Destroy(body);
            }

            if (handL != null)
            {
                GameObject.Destroy(handL);
                GameObject.Destroy(handR);
                GameObject.Destroy(footL);
                GameObject.Destroy(footR);
                GameObject.Destroy(footLBend);
                GameObject.Destroy(footRBend);
            }

            if (player != null)
            {
                GameObject.Destroy(player);
            }
        }
    }
}
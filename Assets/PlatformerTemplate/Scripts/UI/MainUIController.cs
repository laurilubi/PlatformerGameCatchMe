using Platformer.Mechanics;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Platformer.UI
{
    /// <summary>
    /// A simple controller for switching between UI panels.
    /// </summary>
    public class MainUIController : MonoBehaviour
    {
        public GameObject[] panels;
        public GameObject[] playerCountButtons;

        private GameController GameController;

        public void SetActivePanel(int index)
        {
            for (var i = 0; i < panels.Length; i++)
            {
                var active = i == index;
                var g = panels[i];
                if (g.activeSelf != active) g.SetActive(active);
            }
        }

        void OnEnable()
        {
            GameController = GameController.Instance;

            SetActivePanel(0);
        }

        //void Show()
        //{
        //    VisualizePlayerCount();
        //}

        public void SetPlayerCount(int count)
        {
            GameController.model.activePlayerCount = count;
            VisualizePlayerCount();
            StartGame();
        }

        public void VisualizePlayerCount()
        {
            var count = GameController.model.activePlayerCount;

            var button = playerCountButtons[count];
            button.GetComponent<Button>().Select();
        }

        public void SetLevel(int level)
        {
            var gameArea = GameObject.Find("GameArea");
            var camera = GameObject.Find("Main Camera");

            switch (level)
            {
                case 1:
                    gameArea.transform.localPosition = new Vector3(-9.86f, 12.85f, 0);
                    break;
                case 2:
                    gameArea.transform.localPosition = new Vector3(4.35f, 12.85f, 0);
                    break;
                case 3:
                    gameArea.transform.localPosition = new Vector3(18.34f, 12.85f, 0);
                    break;
                default:
                    throw new Exception($"Invalid level {level}");
            }

            camera.transform.localPosition = new Vector3(gameArea.transform.localPosition.x + 11.51f, gameArea.transform.localPosition.y - 5.4f, -9f);

            StartGame();
        }

        public void StartGame()
        {
            GameController.OnEnable();
            //GameController.gameObject.SetActive(false);
            //GameController.gameObject.SetActive(true);

            //this.gameObject.SetActive(false);
        }
    }
}
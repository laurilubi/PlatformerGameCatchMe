using Platformer.Mechanics;
using JetBrains.Annotations;
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

        [UsedImplicitly]
        void OnEnable()
        {
            GameController = GameController.Instance;

            SetActivePanel(0);
        }

        //void Show()
        //{
        //    VisualizePlayerCount();
        //}

        [UsedImplicitly]
        public void SetPlayerCount(int count)
        {
            GameController.SetupPlayers(count);
            VisualizePlayerCount();
        }

        public void VisualizePlayerCount()
        {
            var count = GameController.model.activePlayerCount;

            var button = playerCountButtons[count];
            button.GetComponent<Button>().Select();
        }

        [UsedImplicitly]
        public void SetLevel(int level)
        {
            GameController.SetupLevel(level);
        }
    }
}
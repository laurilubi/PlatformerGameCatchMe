using System.Collections.Generic;
using System.Linq;
using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This class exposes the the game model in the inspector, and ticks the
    /// simulation.
    /// </summary> 
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        //This model field is public and can be therefore be modified in the 
        //inspector.
        //The reference actually comes from the InstanceRegister, and is shared
        //through the simulation and events. Unity will deserialize over this
        //shared reference when the scene loads, allowing the model to be
        //conveniently configured inside the inspector.
        public PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public void OnEnable()
        {
            Instance = this;

            var activePlayers = new List<PlayerController>();
            for (var i = 0; i < model.players.Length; i++)
            {
                if (i < model.activePlayerCount)
                {
                    if (model.players[i].gameObject.activeSelf == false) model.players[i].gameObject.SetActive(true);
                    model.players[i].TeleportRandom();
                    activePlayers.Add(model.players[i]);
                }
                else
                {
                    model.players[i].gameObject.SetActive(false);
                }
            }
            model.activePlayers = activePlayers.ToArray();

            var catcher = model.activePlayers.Skip(Random.Range(0, model.activePlayers.Length)).FirstOrDefault();
            catcher?.MakeCatcher(null);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Instance == this) Simulation.Tick();
        }
    }
}
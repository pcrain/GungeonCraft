using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Alexandria.cAPI
{
    class HatControllerAdder : MonoBehaviour
    {
        public void Update()
        {
            PlayerController player1 = GameManager.Instance.PrimaryPlayer;
            PlayerController player2 = GameManager.Instance.SecondaryPlayer;
            if (player1 && player1.GetComponent<HatController>() == null)
            {
                player1.gameObject.GetOrAddComponent<HatController>();
            }
            if (player2 && player2.GetComponent<HatController>() == null)
            {
                player2.gameObject.GetOrAddComponent<HatController>();
            }
        }
    }

}

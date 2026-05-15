using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TFI.Interactives_Template.ID101
{
    public class Trophy : MonoBehaviour
    {
        public void AddScore()
        {
            FindObjectOfType<Game>().AddScore();
        }
    }
}
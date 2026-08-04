using System;
using UnityEngine;
using UnityEngine.UI;

namespace DZ.Features
{
    public class GameplayView : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        
        public Button BackButton => backButton;
    }
}

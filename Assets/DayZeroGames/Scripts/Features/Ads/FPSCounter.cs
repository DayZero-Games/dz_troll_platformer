using UnityEngine;

namespace DZ.Features
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI _fpsText;

        // Update is called once per frame
        void Update()
        {
            _fpsText.text = Application.targetFrameRate.ToString();
        }
    }
}

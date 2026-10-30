using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuildMania.XR
{
    /// <summary>
    /// In the mixed reality scene, switches to the virtual workshop when the device or
    /// connection cannot show passthrough, so the game always has a usable view.
    /// </summary>
    public sealed class PassthroughFallback : MonoBehaviour
    {
        [SerializeField] string fallbackScene = "VRWorkshop";

        void Start()
        {
            if (!OVRManager.IsInsightPassthroughSupported() && !string.IsNullOrEmpty(fallbackScene))
            {
                Debug.Log($"Passthrough is not available here; loading {fallbackScene}.");
                SceneManager.LoadScene(fallbackScene);
            }
        }
    }
}

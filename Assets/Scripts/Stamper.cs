using UnityEngine;

public class Stamper : MonoBehaviour
{
    [SerializeField] ParticleSystem stampGreenPS;
    [SerializeField] ParticleSystem stampRedPS;

    public void PlayStampEffect(bool isApproved)
    {
        if (isApproved)
        {
            stampGreenPS.Play();
        }
        else
        {
            stampRedPS.Play();
        }
    }
}

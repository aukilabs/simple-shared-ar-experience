using UnityEngine;

public class TouchableByHand : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "hand")
        {
            gameObject.GetComponent<Renderer>().material.color = Random.ColorHSV();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "hand")
        {
            gameObject.GetComponent<Renderer>().material.color = Color.white;
        }
    }
}
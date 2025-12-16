using UnityEngine;

public class SpinnerRotator : MonoBehaviour
{
    public float speed = 200f;

    void Update()
    {
        // Ruota sull'asse Z (come una lancetta)
        transform.Rotate(0, 0, -speed * Time.deltaTime);
    }
}
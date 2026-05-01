using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float minX = -2f;
    public float maxX = 2f;
    public float speed = 2f;
    private bool movingRight = true;
    private float startX;
    private bool initialized = false;

    private void Update()
    {
        if (!initialized)
        {
            startX = transform.position.x;
            initialized = true;
        }

        float targetX = movingRight ? startX + maxX : startX + minX;
        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, targetX, speed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Abs(pos.x - (startX + maxX)) < 0.01f)
            movingRight = false;
        else if (Mathf.Abs(pos.x - (startX + minX)) < 0.01f)
            movingRight = true;
    }
}

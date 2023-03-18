using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public float coreVelocity = 10.0f;

    void Update()
    {
        var velocity = coreVelocity;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            velocity *= 10.0f;
        }

        if (Input.GetKey(KeyCode.W))
        {
            this.transform.Translate(new Vector3(0, 0, velocity * Time.deltaTime));
        }

        if (Input.GetKey(KeyCode.S))
        {
            this.transform.Translate(new Vector3(0, 0, -velocity * Time.deltaTime));
        }

        if (Input.GetKey(KeyCode.Space))
        {
            this.transform.Translate(new Vector3(0, velocity * Time.deltaTime, 0));
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            this.transform.Translate(new Vector3(0, -velocity * Time.deltaTime, 0));

        }

        if (Input.GetKey(KeyCode.A))
        {
            this.transform.Translate(new Vector3(-velocity * Time.deltaTime, 0, 0));
        }

        if (Input.GetKey(KeyCode.D))
        {
            this.transform.Translate(new Vector3(velocity * Time.deltaTime, 0, 0));
        }

        if (Input.GetMouseButton(1))
        {
            var yAxis = Input.GetAxis("Mouse X");
            var xAxis = -Input.GetAxis("Mouse Y");
            this.transform.eulerAngles += 1.5f * new Vector3(xAxis, yAxis, 0);
        }

    }
    private void OnGUI()
    {
        //GUI.Box(new Rect(Screen.width / 2, Screen.height / 2, 10, 10), "");
    }
}

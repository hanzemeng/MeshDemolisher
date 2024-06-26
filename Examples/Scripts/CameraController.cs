using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotateSpeed;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.A))
        {
            transform.position -= Time.deltaTime*moveSpeed*transform.right;
        }
        else if(Input.GetKey(KeyCode.D))
        {
            transform.position += Time.deltaTime*moveSpeed*transform.right;
        }
        if(Input.GetKey(KeyCode.S))
        {
            transform.position -= Time.deltaTime*moveSpeed*transform.forward;
        }
        else if(Input.GetKey(KeyCode.W))
        {
            transform.position += Time.deltaTime*moveSpeed*transform.forward;
        }
        if(Input.GetKey(KeyCode.Q))
        {
            transform.position -= Time.deltaTime*moveSpeed*transform.up;
        }
        else if(Input.GetKey(KeyCode.E))
        {
            transform.position += Time.deltaTime*moveSpeed*transform.up;
        }

        if(Input.GetMouseButton(1))
        {
            float x = Time.deltaTime * rotateSpeed * Input.GetAxis("Mouse X");
            float y = Time.deltaTime * rotateSpeed * Input.GetAxis("Mouse Y");
            x = Mathf.Clamp(x, -10f, 10f);
            y = Mathf.Clamp(y, -10f, 10f);

            transform.Rotate(0f, x, 0f, Space.World);
            transform.Rotate(-y, 0f, 0f, Space.Self);
        }
    }
}

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgeDetection : MonoBehaviour
{
    BoxCollider2D edgeCollider;
    private bool _isAtEdge = false;
    public List<Collider2D> colliders;
    public bool IsAtEdge {
        get {
            return _isAtEdge;
        }
    }

    private void Awake() {
        edgeCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collider) {
        _isAtEdge = false;
        colliders.Add(collider);
    }

    private void OnTriggerExit2D(Collider2D collider) {
        _isAtEdge = true;
        colliders.Clear();
    }

    public void FlipEdgeDetectorToRollDirection(int flipDirection) {
        Debug.Log("FlipDirectionEntered");
        if (flipDirection > 0 && transform.localPosition.x < 0) transform.localPosition = new Vector3(transform.localPosition.x * -1, transform.localPosition.y, transform.localPosition.z);
        else if (flipDirection < 0 && transform.localPosition.x > 0) transform.localPosition = new Vector3(transform.localPosition.x * -1, transform.localPosition.y, transform.localPosition.z);
    }

}

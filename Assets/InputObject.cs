using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputObject : MonoBehaviour
{
    private string _type;
    public string Type { 
        get {
            return _type;
        } 
        set {
            _type = value;
        } 
    }

    private Vector2 _vector;
    public Vector2 Vector { 
        get {
            return _vector;
        } 
        set {
            _vector = new Vector2(value.x, value.y);
        } 
    }

    /*public InputObject() {
        Type = "move";
        Vector = new Vector2(0, 0);
    }*/
}

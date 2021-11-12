using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    private Rigidbody2D _myRb;
    private BoxCollider2D _myCollider;
    private Conductor _conductor;
    private SmoothSprite _mySR;

    private Room _curRoom;
    
    private bool _wasHoldingZ;
    private Machine _prevMachine;

    // Start is called before the first frame update
    void Start() {
        _myRb = GetComponent<Rigidbody2D>();
        _myCollider = GetComponent<BoxCollider2D>();
        _mySR = GetComponentInChildren<SmoothSprite>();
        _conductor = FindObjectOfType<Conductor>();
    }
    
    // Update is called once per frame
    void Update() {
        int inputH = checkInputH();
        int inputV = checkInputV();
        Vector3 newPos = Vector3.zero;
        bool moved = false;
        bool attemptMove = inputH != 0 || inputV != 0;


        if (attemptMove) {
            bool onBeat = _conductor.AttemptMove();
            if (onBeat) {
                if (inputH != 0) {
                    newPos = _myRb.position + new Vector2(inputH, 0);
                }

                if (inputV != 0) {
                    newPos = _myRb.position + new Vector2(0, inputV);
                }

                _mySR.Move(newPos);
                _myRb.MovePosition(newPos);
                moved = true;
            }
        }

        bool curZ = Input.GetKey(KeyCode.Z);
        if (curZ) {
            if (!_wasHoldingZ) {
                _prevMachine = onMachine(transform.position);
            } else {
                if (moved && _prevMachine != null) {
                    Machine outMachine = onMachine(newPos);
                    if (outMachine == null) {
                        Vector3 direction = newPos-transform.position;
                        float angleRot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        Quaternion rotation = Quaternion.Euler(0, 0, angleRot);
                        Vector2 conveyorPos = new Vector2(newPos.x, newPos.y);
                        outMachine = _conductor.InstantiateConveyor(conveyorPos, rotation);
                    }
                    Vector3 portPos = (transform.position + newPos) / 2;
                    outMachine.AddInputMachine(_prevMachine, portPos);
                    _prevMachine.AddOutputMachine(outMachine, portPos);
                    _prevMachine = outMachine;
                }
            }
        }

        _wasHoldingZ = curZ;

        if (_conductor.SongIsOnBeat()) {
            _mySR.GetComponent<SpriteRenderer>().color = Color.red;
        }
        else {
            _mySR.GetComponent<SpriteRenderer>().color = Color.blue;
        }
    }

    private void FixedUpdate() {
        Collider2D roomCollider = CheckRoomOverlap();
        if (roomCollider) {
            if (!_curRoom) {
                _curRoom = roomCollider.GetComponent<Room>();
                _curRoom.OnPlayerEnter(this);
            }
        } else {
            if (_curRoom) {
                _curRoom.OnPlayerExit(this);
            }

            _curRoom = null;
        }
    }

    private Collider2D CheckRoomOverlap() {
        //So the player touching the edge of the collider isn't counted as an overlap
        Vector3 alpha = new Vector3(0.05f, 0.05f);
        Vector2 topLeftCorner = _myCollider.bounds.min + alpha;
        Vector2 topRightCorner = _myCollider.bounds.max - alpha;
        Collider2D overlapCollider = Physics2D.OverlapArea(
            topLeftCorner, 
            topRightCorner,
            LayerMask.GetMask("Room")
        );
        return overlapCollider;
    }
    
    Machine onMachine(Vector3 pos) {
        RaycastHit2D hit =
            Physics2D.Raycast(
                pos, 
                new Vector3(0, 0, 1), 
                10.0f, 
                LayerMask.GetMask("Machine"));
        if (hit.transform != null) {
            return hit.transform.GetComponent<Machine>();
        } else {
            return null;
        }
    }

    int checkInputH() {
        bool leftPress = Input.GetKeyDown("left");
        bool rightPress = Input.GetKeyDown("right");
        return leftPress ? -1 : (rightPress ? 1 : 0);
    }
    
    int checkInputV() {
        bool upPress = Input.GetKeyDown("up");
        bool downPress = Input.GetKeyDown("down");
        return upPress ? 1 : (downPress ? -1 : 0);
    }
    void OnDrawGizmos() {
        // if (_conductor) { 
            // Handles.Label(transform.position, ""+_conductor.SongIsOnBeat());
        // }
        if(_prevMachine) Handles.Label(_prevMachine.transform.position, "Prev");
        if(_conductor) Handles.Label(transform.position, _conductor.CurCombo+"");
    }
}
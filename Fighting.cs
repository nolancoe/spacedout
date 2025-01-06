using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Fighting : MonoBehaviour
{
    //Avoiding strings when setting animator triggers
    private static readonly int LeftJab = Animator.StringToHash("LeftJab");
    private static readonly int RightJab = Animator.StringToHash("RightJab");

    ///Object references
    private ThirdPersonController _thirdPersonController;
    private Animator _animator;
    [SerializeField] private CapsuleCollider leftHandCollider;
    [SerializeField] private CapsuleCollider rightHandCollider;
    public bool isPunching;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _thirdPersonController = GetComponent<ThirdPersonController>();
        if (_thirdPersonController == null)
        {
            Debug.LogError("ThirdPersonController not found on the GameObject.");
        }
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!_thirdPersonController || !_thirdPersonController._isFighting) return;
        
    }

    public void OnLeftJab(InputValue value)
    {
        if (!_thirdPersonController._isFighting || _thirdPersonController._isCrouching) return;
        if (value.isPressed)
        {
            _animator.SetTrigger(LeftJab);
            isPunching = true;
            StartCoroutine(IsPunchingReset());
        }
        
    }
    
    public void OnRightJab(InputValue value)
    {
        if (!_thirdPersonController._isFighting || _thirdPersonController._isCrouching) return;
        if (value.isPressed)
        {
            _animator.SetTrigger(RightJab);
            isPunching = true;
            StartCoroutine(IsPunchingReset());
        }
        
    }

    private IEnumerator IsPunchingReset()
    {
        yield return new WaitForSeconds(0.5f);
        isPunching = false;
    }
}
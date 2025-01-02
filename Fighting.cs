using UnityEngine;
using UnityEngine.InputSystem;

public class Fighting : MonoBehaviour
{
    //Avoiding strings when setting animator triggers
    private static readonly int LeftJab = Animator.StringToHash("LeftJab");

    ///Object references
    private ThirdPersonController _thirdPersonController;
    public Animator _animator;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _thirdPersonController = GetComponent<ThirdPersonController>();
        if (_thirdPersonController == null)
        {
            Debug.LogError("ThirdPersonController not found on the GameObject.");
        }
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_thirdPersonController == null || !_thirdPersonController._isFighting) return;
        
    }

    public void OnLeftJab(InputValue value)
    {
        if (!_thirdPersonController._isFighting) return;
        if (value.isPressed)
        {
            _animator.SetTrigger(LeftJab);
        }
        
    }
}

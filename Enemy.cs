using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private CapsuleCollider headCollider;
    private Animator _animator;

    // Start is called before the first execution of Update
    private void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("Animator not found on the Enemy GameObject.");
        }
    }

    // Detect when a collider enters the headCollider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            Debug.Log("Enemy head hit by: " + other.name);
            _animator.SetTrigger("Hit");
        }
        else
        {
            Debug.Log("not hands");
        }
    }
}
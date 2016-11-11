using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerLegsAnimator : MonoBehaviour
{
    public FollowPath path;
    public float speedMultiplier = 1f;

    private Animator animator;

    private static readonly int SpeedId = Animator.StringToHash("Speed");

    void OnEnable()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Update the animator parameters
        animator.SetFloat(SpeedId, path.Speed * speedMultiplier);
    }
}

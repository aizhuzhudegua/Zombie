using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class ThirdPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 2.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 5.335f;
		[Tooltip("How fast the character turns to face movement direction")]
		[Range(0.0f, 0.3f)]
		public float RotationSmoothTime = 0.12f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;
        public float Sensitivity = 1f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.50f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.28f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 70.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -30.0f;
		[Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
		public float CameraAngleOverride = 0.0f;
		[Tooltip("For locking the camera position on all axis")]
		public bool LockCameraPosition = false;

		// cinemachine
		private float _cinemachineTargetYaw;
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _targetRotation = 0.0f;
		private float _rotationVelocity;
		private float _verticalVelocity;

		private Animator _animator;
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		public GameObject MainCamera;
        private bool _rotateOnMove = true;

		private const float _threshold = 0.01f;

		private ThirdPersonShooterController shooterController;

		private void Awake()
		{
			// get a reference to our main camera
			if (MainCamera == null)
			{
				MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
			shooterController = GetComponent<ThirdPersonShooterController>();
			_animator = GetComponent<Animator>();
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();

		}

		private void Update()
		{
			CheckGround();
			Move();
		}
		
		private void CheckGround()
		{
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void CameraRotation()
		{
			// if there is an input and camera position is not fixed
			if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
			{
				_cinemachineTargetYaw += _input.look.x * Time.deltaTime * Sensitivity;
                _cinemachineTargetPitch += _input.look.y * Time.deltaTime * Sensitivity;
            }

			// clamp our rotations so our values are limited 360 degrees
			_cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			// Cinemachine will follow this target
			CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
		}


		private float InputX = 0;
		private float InputY = 0;
		private void Move()
		{
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
			;
			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				
				shooterController.noise.m_FrequencyGain = Mathf.Lerp(shooterController.noise.m_FrequencyGain, 5f, Time.deltaTime * 2f);  // 镜头晃动
				_animator.SetBool("IsMove", true);
				_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + MainCamera.transform.eulerAngles.y;
				float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                // rotate to face input direction relative to camera position
                if (_rotateOnMove) 
                {
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
			}
			else
            {
		

				shooterController.noise.m_FrequencyGain = Mathf.Lerp(shooterController.noise.m_FrequencyGain, 0.5f, Time.deltaTime * 2f);
				_animator.SetBool("IsMove", false);
			}

			// 平滑阻尼（不会震荡！）
			float smoothSpeed = 5f; // 调大更快，调小更柔

			InputX = Mathf.MoveTowards(InputX, _input.move.x, Time.deltaTime * smoothSpeed);
			InputY = Mathf.MoveTowards(InputY, _input.move.y, Time.deltaTime * smoothSpeed);

			// 特别接近时直接等于目标，彻底消除抖动
			if (Mathf.Abs(InputX - _input.move.x) < 0.01f) InputX = _input.move.x;
			if (Mathf.Abs(InputY - _input.move.y) < 0.01f) InputY = _input.move.y;
			// ======================================================

			_animator.SetFloat("InputX", InputX);
			_animator.SetFloat("InputY", InputY);

		}

        private void OnAnimatorMove()
        {
			_speed = _animator.deltaPosition.magnitude / Time.deltaTime;
			Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
			
			// 获取动画的垂直位移（处理跳跃/下落）
			float verticalVelocity = _animator.deltaPosition.y / Time.deltaTime;
			
			// 应用重力（如果角色不在地面上）
			// if (!Grounded)
			// {
			// 	_verticalVelocity += Gravity * Time.deltaTime;
			// }
			// else
			// {
			// 	_verticalVelocity = verticalVelocity;
			// }
			_verticalVelocity += Gravity * Time.deltaTime;
			
			// 应用移动
			Vector3 move = targetDirection.normalized * (_speed * Time.deltaTime);
			move.y = _verticalVelocity * Time.deltaTime;
			_controller.Move(move);
		}


		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;
			
			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}

        public void SetSensitivity(float newSensitivity) 
        {
            Sensitivity = newSensitivity;
        }

        public void SetRotateOnMove(bool newRotateOnMove) 
        {
            _rotateOnMove = newRotateOnMove;
        }
	}
}
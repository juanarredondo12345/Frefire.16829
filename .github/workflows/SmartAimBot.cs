using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Aimbot Avanzado para Free Fire / Unity
/// Características: FOV Circular, Distancia Max, Lerp Suave, Raycast Anti-Paredes
/// </summary>
public class SmartAimBot : MonoBehaviour
{
    [Header("Configuración General")]
    public Transform cameraTransform; // Asignar desde Inspector (Main Camera)
    public Transform playerBody;      // Asignar desde Inspector (Tu personaje)
    public float fovAngle = 90f;      // Ángulo de visión del aimbot
    public float maxDistance = 50f;   // Distancia máxima para detectar enemigos
    public float smoothSpeed = 8f;    // Velocidad de giro (más alto = más rápido/rápido)
    public KeyCode toggleKey = KeyCode.LeftShift; // Tecla para activar/desactivar
    public bool aimOnHold = true;     // Si es True, solo apunta mientras mantienes la tecla

    [Header("Capas de Enemigos")]
    public LayerMask enemyLayer;      // Asignar capa "Enemy" o "Player" en Unity

    private bool isAimingActive = false;
    private Vector3 currentLookDirection;
    private Vector3 targetPoint;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        
        // Inicializar dirección actual basada en la cámara
        currentLookDirection = cameraTransform.forward;
    }

    void Update()
    {
        // 1. Verificar si debemos activar el aim
        if ((aimOnHold && Input.GetKey(toggleKey)) || !aimOnHold)
        {
            isAimingActive = true;
        }
        else
        {
            isAimingActive = false;
        }

        if (isAimingActive)
        {
            FindAndTrackTarget();
        }
        else
        {
            // Volver suavemente a la dirección original de la cámara cuando se suelta
            Vector3 camForward = cameraTransform.forward;
            currentLookDirection = Vector3.Slerp(currentLookDirection, camForward, Time.deltaTime * smoothSpeed);
        }

        // 2. Aplicar la rotación al cuerpo del jugador (Yaw) y a la cámara (Pitch)
        ApplyRotation();
    }

    /// <summary>
    /// Encuentra el enemigo más cercano dentro del FOV y distancia
    /// </summary>
    private void FindAndTrackTarget()
    {
        List<Transform> targets = new List<Transform>();
        
        // Obtener todos los enemigos activos en la escena (ajusta el tag o layer según tu juego)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); 
        // Si usas Layers en lugar de Tags, usa: GameObject.FindGameObjectsWithTag("") y filtra por layer
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy.layer != enemyLayer.value) continue;

            Transform t = enemy.transform;
            
            // Distancia
            float dist = Vector3.Distance(transform.position, t.position);
            if (dist > maxDistance) continue;

            // Dirección relativa al jugador
            Vector3 dirToEnemy = (t.position - transform.position).normalized;
            
            // Dirección actual de mira del jugador (sin contar pitch vertical para el FOV circular base)
            Vector3 playerFlatDir = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
            
            // Ángulo entre donde miras y dónde está el enemigo
            float angle = Vector3.Angle(playerFlatDir, dirToEnemy);

            // Si está dentro del FOV circular
            if (angle <= fovAngle / 2f)
            {
                // Raycast para asegurar que no haya paredes entre tú y el enemigo
                if (!Physics.Linecast(transform.position, t.position, enemyLayer | LayerMask.GetMask("Default")))
                {
                    targets.Add(t);
                }
            }
        }

        if (targets.Count > 0)
        {
            // Seleccionar el objetivo más cercano
            Transform closestTarget = targets.MinBy(t => Vector3.Distance(transform.position, t.position));
            
            // Punto de mira: Cabeza o centro del torso
            targetPoint = closestTarget.position;
            
            // Ajustar para apuntar a la cabeza (si conoces la altura)
            // targetPoint.y += 1.5f; 

            // Calcular dirección ideal hacia el punto de mira
            Vector3 directionToTarget = (targetPoint - transform.position).normalized;
            
            // Interpolación suave (Lerp) para que el movimiento no sea instantáneo (menos detectable)
            currentLookDirection = Vector3.Slerp(currentLookDirection, directionToTarget, Time.deltaTime * smoothSpeed);
        }
    }

    /// <summary>
    /// Aplica la rotación calculada al cuerpo y a la cámara
    /// </summary>
    private void ApplyRotation()
    {
        // Rotar el cuerpo del jugador (Yaw)
        Quaternion targetRotation = Quaternion.LookRotation(currentLookDirection);
        // Solo rotamos en Y (eje vertical), mantenemos X e Z fijos si es necesario
        playerBody.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);

        // Opcional: Rotar la cámara ligeramente hacia arriba/abajo si quieres que el crosshair siga al objetivo
        // Esto es más avanzado y puede requerir ajustar el "Mouse Sensitivity"
    }
}

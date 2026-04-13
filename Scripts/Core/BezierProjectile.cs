using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Projétil inteligente com trajetória Bézier Quadrática ou Cúbica.
///
/// Funcionalidades:
///   • Bézier Quadrática e Cúbica com equações completas e suas derivadas
///   • Homing dinâmico (rastreia alvos em movimento)
///   • Leading: antecipa a posição futura do alvo com base em sua velocidade
///   • Auto-Targeting via Physics.OverlapSphere (IDamageable)
///   • Chain Lightning: encadeia para o próximo alvo após o impacto
///   • Colisão real via OnTriggerEnter (requer Collider trigger + Rigidbody kinematic)
///   • Timeout configurável com evento dedicado
///   • Object Pooling integrado (BezierProjectilePool)
///   • AnimationCurve de easing de velocidade
///   • Dano via interface IDamageable (desacoplado)
///   • VFX de impacto via prefab
///   • TrailRenderer configurável por ProjectileData
///   • Lógica de voo em Coroutine (sem Update)
///
/// Requisitos do GameObject:
///   • Rigidbody (IsKinematic = true, Use Gravity = false)
///   • Collider com Is Trigger = true
///   • (Opcional) TrailRenderer
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BezierProjectile : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspetor
    // -------------------------------------------------------------------------

    [Header("Dados do Projétil")]
    [Tooltip("ScriptableObject com todas as configurações. Crie via Assets > Create > Projectile > Data.")]
    public ProjectileData data;

    [Header("Eventos")]
    [Tooltip("Disparado ao iniciar o voo.")]
    public UnityEvent onLaunch;

    [Tooltip("Disparado ao atingir o alvo ou colidir.")]
    public UnityEvent onImpact;

    [Tooltip("Disparado caso o projétil expire sem atingir o alvo.")]
    public UnityEvent onTimeout;

    // -------------------------------------------------------------------------
    // Pool — preenchido pelo BezierProjectilePool ao criar a instância
    // -------------------------------------------------------------------------

    [HideInInspector] public BezierProjectilePool Pool;

    // -------------------------------------------------------------------------
    // Estado interno de voo
    // -------------------------------------------------------------------------

    private float     currentT;
    private bool      hasArrived;
    private float     damageMultiplier = 1f;   // reduzido por chain
    private int       chainDepth;
    private Coroutine flightCoroutine;

    private Transform target;
    private Vector3   startPoint;
    private Vector3   endPoint;              // ponto final cacheado (atualizado em homing)
    private Vector3   controlPoint1;
    private Vector3   controlPoint2;

    private Vector3   lastTargetPos;
    private Vector3   targetVelocity;

    private TrailRenderer trail;

    // -------------------------------------------------------------------------
    // Unity Messages
    // -------------------------------------------------------------------------

    void Awake()
    {
        // Garante que física não interfira com a matemática da curva
        Rigidbody rb  = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        trail = GetComponent<TrailRenderer>();
    }

    /// <summary>
    /// Colisão física: interrompe o voo e aplica impacto ao objeto colidido.
    /// Requer Collider (Is Trigger = true) no GameObject.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (hasArrived || data == null) return;

        // Filtra por máscara de colisão configurada no ProjectileData
        if ((data.collisionMask.value & (1 << other.gameObject.layer)) == 0) return;

        TriggerImpact(other.gameObject);
    }

    // -------------------------------------------------------------------------
    // API pública
    // -------------------------------------------------------------------------

    /// <summary>
    /// Lança o projétil em direção a um Transform específico.
    /// </summary>
    /// <param name="target">Alvo do projétil.</param>
    /// <param name="chainDepth">Profundidade de chain atual (0 = disparo primário).</param>
    public void Launch(Transform target, int chainDepth = 0)
    {
        if (data == null)
        {
            Debug.LogError("[BezierProjectile] ProjectileData não atribuído.");
            return;
        }

        this.target     = target;
        this.chainDepth = chainDepth;
        Initialize();
    }

    /// <summary>
    /// Busca automaticamente o IDamageable mais próximo dentro do raio configurado
    /// em ProjectileData e lança o projétil em sua direção.
    /// </summary>
    public void LaunchAutoTarget(int chainDepth = 0)
    {
        if (data == null)
        {
            Debug.LogError("[BezierProjectile] ProjectileData não atribuído.");
            return;
        }

        Transform found = FindNearestDamageable(
            transform.position, data.autoTargetRadius, data.autoTargetMask);

        if (found == null)
        {
            Debug.LogWarning("[BezierProjectile] Nenhum alvo encontrado no raio de auto-targeting.");
            ReturnToPool();
            return;
        }

        Launch(found, chainDepth);
    }

    // -------------------------------------------------------------------------
    // Inicialização
    // -------------------------------------------------------------------------

    private void Initialize()
    {
        currentT         = 0f;
        hasArrived       = false;
        startPoint       = transform.position;
        lastTargetPos    = target.position;
        targetVelocity   = Vector3.zero;

        RecalculateControlPoints(target.position);
        ConfigureTrail();

        // Para qualquer coroutine remanescente de uso anterior (pool)
        if (flightCoroutine != null)
        {
            StopCoroutine(flightCoroutine);
            flightCoroutine = null;
        }

        flightCoroutine = StartCoroutine(FlightCoroutine());
        onLaunch?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Coroutine de Voo — substitui Update para controle mais limpo do ciclo de vida
    // -------------------------------------------------------------------------

    private IEnumerator FlightCoroutine()
    {
        float elapsed = 0f;

        while (currentT < 1f && elapsed < data.timeout && !hasArrived)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            // Rastreia velocidade do alvo a cada frame (necessário para leading)
            if (target != null)
            {
                targetVelocity = (target.position - lastTargetPos) / Mathf.Max(dt, 0.0001f);
                lastTargetPos  = target.position;

                // Homing: recalcula a curva com posição atual ou prevista do alvo
                if (data.homing)
                {
                    Vector3 aimPos = data.leadTarget
                        ? PredictTargetPosition()   // Leading: onde o alvo estará
                        : target.position;          // Homing puro: onde o alvo está agora
                    RecalculateControlPoints(aimPos);
                }
            }

            // Easing: multiplica velocidade base pelo valor da AnimationCurve em t
            float easedSpeed = data.speed * data.speedCurve.Evaluate(currentT);
            currentT += dt * easedSpeed;
            currentT  = Mathf.Clamp01(currentT);

            // Posiciona e orienta o projétil na tangente da curva
            transform.position = SampleCurve(currentT);
            Vector3 tangent    = SampleTangent(currentT);
            if (tangent.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(tangent);

            yield return null;
        }

        // Saída da coroutine: timeout ou chegada ao alvo
        if (!hasArrived)
        {
            if (currentT < 1f)
            {
                // Expirou sem atingir o alvo
                onTimeout?.Invoke();
                ReturnToPool();
            }
            else
            {
                // Chegou ao final da curva naturalmente
                TriggerImpact(target != null ? target.gameObject : null);
            }
        }

        flightCoroutine = null;
    }

    // -------------------------------------------------------------------------
    // Impacto
    // -------------------------------------------------------------------------

    private void TriggerImpact(GameObject hitObject)
    {
        if (hasArrived) return;
        hasArrived = true;

        // Aplica dano via IDamageable (desacoplado de qualquer classe concreta)
        if (hitObject != null && hitObject.TryGetComponent<IDamageable>(out IDamageable damageable))
            damageable.TakeDamage(data.damage * damageMultiplier, gameObject);

        // VFX no ponto de impacto
        if (data.impactVFXPrefab != null)
            Instantiate(data.impactVFXPrefab, transform.position, Quaternion.identity);

        onImpact?.Invoke();

        // Chain Lightning: encadeia para o próximo alvo
        if (chainDepth < data.chainCount)
            SpawnChain();

        ReturnToPool();
    }

    // -------------------------------------------------------------------------
    // Chain Lightning
    // -------------------------------------------------------------------------

    private void SpawnChain()
    {
        if (Pool == null)
        {
            Debug.LogWarning("[BezierProjectile] Chain requer BezierProjectilePool na cena.");
            return;
        }

        // Exclui o alvo atual para não encadear no mesmo inimigo
        Transform nextTarget = FindNearestDamageable(
            transform.position, data.chainRadius, data.autoTargetMask, exclude: target);

        if (nextTarget == null) return;

        BezierProjectile chained       = Pool.Get();
        chained.data                   = data;
        chained.Pool                   = Pool;
        chained.damageMultiplier       = damageMultiplier * data.chainDamageMultiplier;
        chained.transform.position     = transform.position;
        chained.Launch(nextTarget, chainDepth + 1);
    }

    // -------------------------------------------------------------------------
    // Pool
    // -------------------------------------------------------------------------

    private void ReturnToPool()
    {
        // Limpa o trail para não deixar rastro residual ao reutilizar
        if (trail != null) trail.Clear();

        if (Pool != null)
            Pool.Return(this);   // SetActive(false) para também a coroutine
        else
            Destroy(gameObject);
    }

    // -------------------------------------------------------------------------
    // Matemática — Curvas de Bézier
    // -------------------------------------------------------------------------

    private Vector3 SampleCurve(float t)
    {
        return data.curveType == BezierCurveType.Cubic
            ? BezierCubic(t, startPoint, controlPoint1, controlPoint2, endPoint)
            : BezierQuadratic(t, startPoint, controlPoint1, endPoint);
    }

    private Vector3 SampleTangent(float t)
    {
        return data.curveType == BezierCurveType.Cubic
            ? BezierCubicTangent(t, startPoint, controlPoint1, controlPoint2, endPoint)
            : BezierQuadraticTangent(t, startPoint, controlPoint1, endPoint);
    }

    /// <summary>
    /// Atualiza os pontos de controle e o endPoint com base na posição alvo fornecida.
    /// Em modo Homing, chamado a cada frame da coroutine.
    /// </summary>
    private void RecalculateControlPoints(Vector3 targetPos)
    {
        endPoint = targetPos;

        Vector3 midpoint  = startPoint + (targetPos - startPoint) * 0.5f;
        Vector3 perpRight = Vector3.Cross(
            (targetPos - startPoint).normalized, Vector3.up).normalized;

        // P1: arco para cima — cria o "bojo" do míssil
        controlPoint1 = midpoint + Vector3.up * data.arcHeight;

        // P2 (apenas Cubic): desvio lateral — cria a curva em "S"
        if (data.curveType == BezierCurveType.Cubic)
            controlPoint2 = midpoint + perpRight * data.cubicSideOffset
                          + Vector3.up * (data.arcHeight * 0.5f);
    }

    // Quadrática — B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
    static Vector3 BezierQuadratic(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1f - t;
        return u * u * p0
             + 2f * u * t * p1
             + t  * t * p2;
    }

    // Derivada Quadrática — B'(t) = 2(1-t)(P1-P0) + 2t(P2-P1)
    static Vector3 BezierQuadraticTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        => 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);

    // Cúbica — B(t) = (1-t)³·P0 + 3(1-t)²t·P1 + 3(1-t)t²·P2 + t³·P3
    static Vector3 BezierCubic(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u  = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return uu * u * p0
             + 3f * uu * t  * p1
             + 3f * u  * tt * p2
             + tt * t  * p3;
    }

    // Derivada Cúbica — B'(t) = 3(1-t)²(P1-P0) + 6(1-t)t(P2-P1) + 3t²(P3-P2)
    static Vector3 BezierCubicTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        => 3f * (1f - t) * (1f - t) * (p1 - p0)
         + 6f * (1f - t) * t        * (p2 - p1)
         + 3f * t        * t        * (p3 - p2);

    // -------------------------------------------------------------------------
    // Leading — previsão de posição futura do alvo
    // -------------------------------------------------------------------------

    /// <summary>
    /// Estima onde o alvo estará quando o projétil chegar.
    /// Usa a velocidade rastreada frame-a-frame e o tempo de voo restante.
    /// </summary>
    private Vector3 PredictTargetPosition()
    {
        // Tempo restante estimado com base no progresso atual e velocidade média
        float remainingTime = (1f - currentT) / Mathf.Max(data.speed, 0.0001f);
        return target.position + targetVelocity * remainingTime;
    }

    // -------------------------------------------------------------------------
    // Auto-Targeting
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encontra o Transform com IDamageable mais próximo dentro do raio dado.
    /// </summary>
    /// <param name="exclude">Transform a ignorar na busca (ex: alvo atual ao fazer chain).</param>
    public static Transform FindNearestDamageable(
        Vector3 origin, float radius, LayerMask mask, Transform exclude = null)
    {
        Collider[] hits    = Physics.OverlapSphere(origin, radius, mask);
        Transform  nearest = null;
        float      minDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            if (exclude != null && col.transform == exclude) continue;
            if (!col.TryGetComponent<IDamageable>(out _))    continue;

            float dist = Vector3.Distance(origin, col.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = col.transform;
            }
        }

        return nearest;
    }

    // -------------------------------------------------------------------------
    // Trail
    // -------------------------------------------------------------------------

    private void ConfigureTrail()
    {
        if (trail == null || data == null) return;
        trail.startColor = data.trailStartColor;
        trail.endColor   = data.trailEndColor;
        trail.Clear();
    }

    // -------------------------------------------------------------------------
    // Gizmos — visualização no Scene View (Gizmos deve estar ativado)
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || data == null || endPoint == Vector3.zero) return;

        const int segments = 30;

        // Curva completa
        Gizmos.color = Color.cyan;
        Vector3 prev = startPoint;
        for (int i = 1; i <= segments; i++)
        {
            float   step = i / (float)segments;
            Vector3 next = SampleCurve(step);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Handle do ponto de controle 1
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(controlPoint1, 0.2f);
        Gizmos.DrawLine(startPoint, controlPoint1);
        Gizmos.DrawLine(endPoint,   controlPoint1);

        // Handle do ponto de controle 2 (apenas Cubic)
        if (data.curveType == BezierCurveType.Cubic)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(controlPoint2, 0.2f);
            Gizmos.DrawLine(controlPoint1, controlPoint2);
            Gizmos.DrawLine(endPoint,      controlPoint2);
        }

        // Posição atual do projétil
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.15f);

        // Raio de chain (quando aplicável)
        if (data.chainCount > 0)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, data.chainRadius);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pool para BezierProjectile.
/// Elimina o overhead de Instantiate/Destroy a cada disparo.
///
/// Como usar:
///   1. Adicione este componente em um GameObject vazio na cena.
///   2. Atribua o prefab do projétil no campo "Prefab".
///   3. Para disparar: BezierProjectilePool.Instance.Get()
///   4. O projétil se devolve automaticamente ao pool no impacto ou timeout.
/// </summary>
public class BezierProjectilePool : MonoBehaviour
{
    public static BezierProjectilePool Instance { get; private set; }

    [Tooltip("Prefab do projétil (deve ter o componente BezierProjectile, Rigidbody e Collider).")]
    [SerializeField] private BezierProjectile prefab;

    [Tooltip("Instâncias pré-criadas ao inicializar a cena (evita alocações no primeiro uso).")]
    [SerializeField, Min(1)] private int initialSize = 10;

    private readonly Queue<BezierProjectile> available = new Queue<BezierProjectile>();

    // -------------------------------------------------------------------------
    // Unity Messages
    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (prefab == null)
        {
            Debug.LogError("[BezierProjectilePool] Prefab não atribuído no Inspector.");
            return;
        }

        Prewarm();
    }

    // -------------------------------------------------------------------------
    // API pública
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retira um projétil do pool, ativando-o.
    /// Cria uma nova instância automaticamente se o pool estiver vazio.
    /// </summary>
    public BezierProjectile Get()
    {
        BezierProjectile p = available.Count > 0
            ? available.Dequeue()
            : CreateInstance();

        p.gameObject.SetActive(true);
        return p;
    }

    /// <summary>
    /// Devolve um projétil ao pool, desativando-o.
    /// Chamado automaticamente pelo próprio projétil — não é necessário chamar manualmente.
    /// </summary>
    public void Return(BezierProjectile p)
    {
        p.gameObject.SetActive(false);
        p.transform.SetParent(transform);
        available.Enqueue(p);
    }

    // -------------------------------------------------------------------------
    // Internos
    // -------------------------------------------------------------------------

    private void Prewarm()
    {
        for (int i = 0; i < initialSize; i++)
            Return(CreateInstance());
    }

    private BezierProjectile CreateInstance()
    {
        BezierProjectile p = Instantiate(prefab, transform);
        p.Pool = this;
        p.gameObject.SetActive(false);
        return p;
    }
}

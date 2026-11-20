using UnityEngine;

namespace UltimaLinha.Visual
{
    /// <summary>
    /// Partículas de ambiente em pixel art (quadradinhos na grade de 58 px): poeira no ar, goteira, vapor de cano, faíscas
    /// e neblina rasteira. Tudo configurável no Inspector; só emite com a câmera perto (distância de ativação), para não
    /// gastar partícula fora da tela. Chame Aplica() depois de mudar os campos por código.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class EmissorAmbiente : MonoBehaviour
    {
        public enum Tipo { Poeira, Goteira, Vapor, Faisca, Neblina }

        public Tipo tipo = Tipo.Poeira;
        [Tooltip("Multiplica a quantidade de partículas.")]
        [Range(0f, 4f)] public float intensidade = 1f;
        public Color cor = new Color(0.72f, 0.7f, 0.64f);
        [Range(0f, 1f)] public float opacidade = 0.5f;
        [Tooltip("Multiplica a velocidade das partículas.")]
        public float velocidade = 1f;
        [Tooltip("Área de onde saem (u).")]
        public Vector2 area = new Vector2(6f, 4f);
        [Tooltip("Goteira, vapor e faísca: intervalo entre jatos (s), sorteado entre x e y.")]
        public Vector2 intervalo = new Vector2(1.5f, 4f);
        [Tooltip("Vapor e faísca: quanto dura cada jato (s).")]
        public float duracao = 0.6f;
        [Tooltip("Só emite com a câmera a menos disso (u, no eixo x). 0 = sempre.")]
        public float distanciaAtivacao = 14f;
        public int ordem = 30;
        [Tooltip("Opcional: sprite de cada partícula (neblina usa uma mancha suave; sem ele, quadradinho da grade).")]
        public Sprite textura;

        const float Px = 1f / 58f; // um pixel da grade
        ParticleSystem ps;
        float proximoJato, fimJato;
        Camera cam;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            Aplica();
        }

        void OnValidate()
        {
            if (Application.isPlaying && ps) Aplica();
        }

        bool Continuo => tipo == Tipo.Poeira || tipo == Tipo.Neblina;

        public void Aplica()
        {
            if (!ps) ps = GetComponent<ParticleSystem>();
            var main = ps.main;
            var em = ps.emission;
            var shape = ps.shape;
            var vel = ps.velocityOverLifetime;
            var noise = ps.noise;
            var col = ps.colorOverLifetime;
            var size = ps.sizeOverLifetime;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.maxParticles = 300;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area.x, area.y, 0.01f);
            vel.enabled = false;
            noise.enabled = false;
            size.enabled = false;
            Color c = cor;
            c.a = opacidade;
            main.startColor = c;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.enabled = true;
            col.color = fade;
            switch (tipo)
            {
                case Tipo.Poeira: // cisco flutuando na luz, devagar
                    main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f * velocidade, 0.12f * velocidade);
                    main.startSize = new ParticleSystem.MinMaxCurve(Px, 2f * Px);
                    em.rateOverTime = 3f * intensidade * area.x * area.y / 24f;
                    noise.enabled = true;
                    noise.strength = 0.15f * velocidade;
                    noise.frequency = 0.25f;
                    break;
                case Tipo.Neblina: // névoa rasteira: manchas grandes, quase transparentes, andando de lado
                    main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
                    main.startSpeed = 0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(40f * Px, 90f * Px);
                    em.rateOverTime = 0.6f * intensidade * area.x / 10f;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.World;
                    vel.x = new ParticleSystem.MinMaxCurve(0.08f * velocidade, 0.25f * velocidade);
                    vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    break;
                case Tipo.Goteira: // uma gota cai; o tempo de vida é o da queda até o chão (area.y)
                    main.startLifetime = Mathf.Sqrt(2f * area.y / (9.8f * 1.2f));
                    main.startSpeed = 0f;
                    main.startSize = 2f * Px;
                    main.gravityModifier = 1.2f;
                    shape.scale = new Vector3(Mathf.Max(Px, area.x), Px, 0.01f);
                    shape.position = Vector3.zero;
                    em.rateOverTime = 0f;
                    col.enabled = false;
                    break;
                case Tipo.Vapor: // jato que sobe e abre
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * velocidade, 1.2f * velocidade);
                    main.startSize = new ParticleSystem.MinMaxCurve(4f * Px, 9f * Px);
                    em.rateOverTime = 0f;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 14f;
                    shape.radius = 2f * Px;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
                    noise.enabled = true;
                    noise.strength = 0.3f;
                    noise.frequency = 0.8f;
                    break;
                case Tipo.Faisca: // estouro curto, caindo
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * velocidade, 3.5f * velocidade);
                    main.startSize = new ParticleSystem.MinMaxCurve(Px, 2f * Px);
                    main.gravityModifier = 1.5f;
                    em.rateOverTime = 0f;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 2f * Px;
                    col.enabled = false;
                    break;
            }
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = textura;
            if (textura)
            {
                tsa.mode = ParticleSystemAnimationMode.Sprites;
                while (tsa.spriteCount > 0) tsa.RemoveSprite(0);
                tsa.AddSprite(textura);
            }
            var r = GetComponent<ParticleSystemRenderer>();
            r.sortingOrder = ordem;
            r.renderMode = ParticleSystemRenderMode.Billboard;
        }

        void Start()
        {
            cam = Camera.main;
            proximoJato = Time.time + Random.Range(intervalo.x, intervalo.y);
        }

        void Update()
        {
            if (!cam) cam = Camera.main;
            bool perto = distanciaAtivacao <= 0f || !cam || Mathf.Abs(cam.transform.position.x - transform.position.x) < distanciaAtivacao;
            var em = ps.emission;
            if (Continuo)
            {
                em.enabled = perto;
                return;
            }
            if (!perto || Time.time < proximoJato) return;
            switch (tipo)
            {
                case Tipo.Goteira:
                    ps.Emit(1);
                    proximoJato = Time.time + Random.Range(intervalo.x, intervalo.y);
                    break;
                case Tipo.Faisca:
                    ps.Emit(Mathf.Max(1, Mathf.RoundToInt(Random.Range(5f, 12f) * intensidade)));
                    proximoJato = Time.time + Random.Range(intervalo.x, intervalo.y);
                    break;
                case Tipo.Vapor:
                    if (fimJato < Time.time) fimJato = Time.time + duracao;
                    ps.Emit(Mathf.Max(1, Mathf.RoundToInt(Time.deltaTime * 40f * intensidade)));
                    if (Time.time >= fimJato) proximoJato = Time.time + Random.Range(intervalo.x, intervalo.y);
                    break;
            }
        }
    }
}

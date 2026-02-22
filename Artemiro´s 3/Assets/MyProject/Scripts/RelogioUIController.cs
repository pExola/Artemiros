using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RelogioUIController : MonoBehaviour
{
    [Header("Referência do Personagem")]
    [Tooltip("Arraste o objeto do Artemiro que contém o Animator para cá.")]
    public Animator artemiroAnimator;

    [Header("Configuração das Cores")]
    [Tooltip("Cor para o início do tempo (0%)")]
    public Color corInicial = Color.green;

    [Tooltip("Cor para 25% do tempo")]
    public Color corAmarela = Color.yellow;

    [Tooltip("Cor para 50% do tempo")]
    public Color corLaranja = new Color(1.0f, 0.5f, 0.0f); // Laranja

    [Tooltip("Cor para 75% em diante")]
    public Color corVermelha = Color.red;

    private Image imagemDoRelogio;

    // Variável interna para rastrear em qual "fase" estamos e evitar spam no Animator
    private int faseAtual = -1;

    void Awake()
    {
        imagemDoRelogio = GetComponent<Image>();
        if (imagemDoRelogio != null)
        {
            imagemDoRelogio.color = corInicial;
        }
    }

    public void AtualizarDisplay(float tempoAtual, float tempoMaximo)
    {
        if (imagemDoRelogio == null || tempoMaximo <= 0) return;

        float fillAmount = tempoAtual / tempoMaximo;
        imagemDoRelogio.fillAmount = fillAmount;

        // --- Lógica de Cor com TRANSIÇÃO CONTÍNUA ---
        float porcentagemPassada = 1f - fillAmount;
        Color novaCor;
        int novaFase = 0;

        if (porcentagemPassada < 0.25f)
        {
            // FASE 1: Transição de Verde para Amarelo (0% a 25% do tempo)
            // Normaliza a porcentagem para o intervalo 0-1 dentro desta fase
            float t = porcentagemPassada / 0.25f;
            novaCor = Color.Lerp(corInicial, corAmarela, t);
            novaFase = 0;
        }
        else if (porcentagemPassada < 0.50f)
        {
            // FASE 2: Transição de Amarelo para Laranja (25% a 50% do tempo)
            // Normaliza a porcentagem para o intervalo 0-1 dentro desta fase
            float t = (porcentagemPassada - 0.25f) / 0.25f;
            novaCor = Color.Lerp(corAmarela, corLaranja, t);
            novaFase = 1;
        }
        else if (porcentagemPassada < 0.75f)
        {
            // FASE 3: Transição de Laranja para Vermelho (50% a 75% do tempo)
            // Normaliza a porcentagem para o intervalo 0-1 dentro desta fase
            float t = (porcentagemPassada - 0.50f) / 0.25f;
            novaCor = Color.Lerp(corLaranja, corVermelha, t);
            novaFase = 2;
        }
        else // porcentagemPassada >= 0.75f
        {
            // FASE 4: Fica na cor final (Vermelho) para os últimos 25% do tempo
            novaCor = corVermelha;
            novaFase = 3;
        }

        // Aplica a cor calculada à imagem
        imagemDoRelogio.color = novaCor;

        // --- O SISTEMA RÍGIDO DE SINCRONIZAÇÃO ---
        // Só aciona o Animator se a fase calculada agora for diferente da fase anterior.
        if (faseAtual != novaFase && artemiroAnimator != null)
        {
            faseAtual = novaFase;
            artemiroAnimator.SetInteger("FaseTempo", faseAtual);
        }

    }
}
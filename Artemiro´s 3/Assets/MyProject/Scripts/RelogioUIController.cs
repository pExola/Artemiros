using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RelogioUIController : MonoBehaviour
{
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

        // --- Lógica de Preenchimento (Fill) - Continua a mesma ---
        float fillAmount = tempoAtual / tempoMaximo;
        imagemDoRelogio.fillAmount = fillAmount;

        // --- Lógica de Cor com TRANSIÇÃO CONTÍNUA ---
        float porcentagemPassada = 1f - fillAmount;
        Color novaCor;

        if (porcentagemPassada < 0.25f)
        {
            // FASE 1: Transição de Verde para Amarelo (0% a 25% do tempo)
            // Normaliza a porcentagem para o intervalo 0-1 dentro desta fase
            float t = porcentagemPassada / 0.25f;
            novaCor = Color.Lerp(corInicial, corAmarela, t);
        }
        else if (porcentagemPassada < 0.50f)
        {
            // FASE 2: Transição de Amarelo para Laranja (25% a 50% do tempo)
            // Normaliza a porcentagem para o intervalo 0-1 dentro desta fase
            float t = (porcentagemPassada - 0.25f) / 0.25f;
            novaCor = Color.Lerp(corAmarela, corLaranja, t);
        }
        else if (porcentagemPassada < 0.75f)
        {
            // FASE 3: Transição de Laranja para Vermelho (50% a 75% do tempo)
            // Normaliza a porcentagem para o intervalo 0-1 dentro desta fase
            float t = (porcentagemPassada - 0.50f) / 0.25f;
            novaCor = Color.Lerp(corLaranja, corVermelha, t);
        }
        else // porcentagemPassada >= 0.75f
        {
            // FASE 4: Fica na cor final (Vermelho) para os últimos 25% do tempo
            novaCor = corVermelha;
        }

        // Aplica a cor calculada à imagem
        imagemDoRelogio.color = novaCor;
        }
    }
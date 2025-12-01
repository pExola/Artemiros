using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    [Header("Nome da Cena de Seleção de Fases")]
    public string nomeCenaFases = "Fase Selec";

    public void BotaoPlay()
    {
        SceneManager.LoadScene(nomeCenaFases);
    }

    public void BotaoOptions()
    {
        Debug.Log("Abrir Opções (A implementar)");
    }

    public void BotaoQuit()
    {
        Debug.Log("Saindo do Jogo...");
        Application.Quit();
    }
}
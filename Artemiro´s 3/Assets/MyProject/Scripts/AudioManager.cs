using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    // --- Padr�o Singleton ---
    // Garante que s� existe uma inst�ncia do AudioManager no jogo.
    public static AudioManager Instance;

    [Header("--------- Audio Sources ---------")]
    [Tooltip("Fonte de �udio para a m�sica de fundo.")]
    public AudioSource musicSource;

    [Tooltip("Fonte de �udio para os efeitos sonoros (SFX).")]
    public AudioSource sfxSource;


    [Header("--------- Audio Clips ---------")]
    public AudioClip backgroundMusic;
    // Futuramente, voc� pode adicionar mais clips aqui:
    // public AudioClip clickSFX;
    // public AudioClip matchSFX;
    // public AudioClip winSFX;


    private void Awake()
    {
        // Implementa��o do Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Faz o AudioManager persistir entre as cenas
        }
        else
        {
            Destroy(gameObject); // Se outra inst�ncia for criada, destr�i a nova
        }
    }

    private void Start()
    {
        // Toca a m�sica de fundo assim que o jogo inicia
        PlayMusic(backgroundMusic);
    }

    /// <summary>
    /// Toca uma m�sica de fundo de forma cont�nua (loop).
    /// </summary>
    /// <param name="musicClip">O clipe de m�sica a ser tocado.</param>
    public void PlayMusic(AudioClip musicClip)
    {
        if (musicClip == null)
        {
            Debug.LogWarning("Nenhum clipe de m�sica de fundo foi atribu�do!");
            return;
        }

        musicSource.clip = musicClip;
        musicSource.Play();
    }

    /// <summary>
    /// Toca um efeito sonoro uma �nica vez.
    /// </summary>
    /// <param name="sfxClip">O clipe de SFX a ser tocado.</param>
    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("Nenhum clipe de SFX foi passado para o m�todo PlaySFX!");
            return;
        }

        // Usa PlayOneShot para permitir que v�rios SFX toquem sobrepostos sem se cortar.
        sfxSource.PlayOneShot(sfxClip);
    }
}
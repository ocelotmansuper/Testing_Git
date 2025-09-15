using UnityEngine;

public class MobileGameUI : BaseGameUI
{
    [Header("Mobile Specific Settings")]
    [SerializeField] private float mobileTextScale = 1.5f;

    public override void SetupUI(GameManager manager)
    {
        base.SetupUI(manager);

        // ��������� ����������� ��� ��������� ������
        if (scoreText != null)
            scoreText.fontSize *= mobileTextScale;

        // ����� �������� �������������� ��������� ��� ��������� ������
    }
}

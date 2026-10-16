namespace YuJanggi.UI
{
    public class UIToggle : UIVisible
    {
        public void Toggle()
        {
            if (gameObject.activeSelf)
                Close();
            else
                Open();
        }
        public bool IsToggled => gameObject.activeSelf;
    }
}

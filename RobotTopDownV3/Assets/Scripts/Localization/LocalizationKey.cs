// Auto-generated file. Do not modify manually!
public enum LocalizationKey
{
    menu_audio,
    menu_multiplayer,
    menu_continue,
    menu_quit,
    menu_return,
    menu_new_game,
    menu_options,
}

public static class LocalizationKeyExtensions
{
    public static string ToKey(this LocalizationKey key)
    {
        switch(key)
        {
            case LocalizationKey.menu_audio: return "menu/audio";
            case LocalizationKey.menu_multiplayer: return "menu/multiplayer";
            case LocalizationKey.menu_continue: return "menu/continue";
            case LocalizationKey.menu_quit: return "menu/quit";
            case LocalizationKey.menu_return: return "menu/return";
            case LocalizationKey.menu_new_game: return "menu/new_game";
            case LocalizationKey.menu_options: return "menu/options";
            default: return string.Empty;
        }
    }
}

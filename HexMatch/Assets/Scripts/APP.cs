public static class App
{
    private static GameManager _gameMgr;

    public static GameManager GameMgr
    {
        get
        {
            if (_gameMgr == null)
            {
                if (!Util.TryFindComponent(out _gameMgr))
                    return null;
            }

            return _gameMgr;
        }
    }


}

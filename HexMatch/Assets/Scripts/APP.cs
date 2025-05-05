public static class APP
{
    private static GameManager _gameMgr;

    public static GameManager GameMgr
    {
        get
        {
            if (_gameMgr == null)
            {
                if (!UTIL.TryFindComponent(out _gameMgr))
                    return null;
            }

            return _gameMgr;
        }
    }


}

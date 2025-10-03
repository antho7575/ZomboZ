public static class SaveBackend
{
    public static ISaveDriver Driver;

    public static void Init()
    {
        Driver = new SQLiteSaveDriver(); // Swap for LiteDBSaveDriver if desired
    }
}

using DataAccess.Interfaces;

namespace DataAccess;

public class SimpleDatabaseWriter<T>(ISaveStrategy saveStrategy) : Writer<T>(saveStrategy) where T : class;
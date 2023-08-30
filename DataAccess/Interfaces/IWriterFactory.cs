namespace DataAccess; 

public interface IWriterFactory {
    IWriter GetWriter();
}
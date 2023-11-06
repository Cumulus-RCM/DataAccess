namespace DataAccess.Interfaces; 

public interface IWriterFactory {
    IWriter GetWriter();
}
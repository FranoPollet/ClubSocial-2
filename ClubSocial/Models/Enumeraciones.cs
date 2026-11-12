namespace ClubSocial.Models
{
    //Moneda en la que se cobra o paga algo
    public enum Moneda
    {
        ARS, //Moneda principal
        USD //Dolares
    }

    //Qué es lo que tiene un precio
    public enum TipoConcepto
    {
        Cuota,          // Cuota mensual de una categoría
        Actividad,      // Actividad extra realizada
        Inscripcion     // Inscripción de un socio nuevo
    }
}


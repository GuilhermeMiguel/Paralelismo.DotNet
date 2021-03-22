using System;

namespace BancoSimple.Core.Model
{
    public enum TipoMovimento
    {
        Saque,
        Deposito,
        Transferencia,
        RecargaCelular,
        PagamentoDebito
    }

    public class Movimento
    {
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public TipoMovimento Tipo { get; set; }
    }
}

using BancoSimple.Core.Model;
using System;
using System.Threading;

namespace BancoSimple.Core.Service
{
    public class ContaClienteService
    {
        public string ConsolidarMovimentacao(ContaCliente conta)
        {
            /*
                    Quero gerar uma sobrecarga do metodo 
                    Para nao quebrar codificações antigas que apontam pra ele (pois esta dentro de uma biblioteca -- assim como o PPO da iob), 
                e também, não repetir codigo, o metodo com a assinatura antiga ira apontar para o novo que tem o novo paramento.

            */

            return ConsolidarMovimentacao(conta, CancellationToken.None);
        }

        public string ConsolidarMovimentacao(ContaCliente conta, CancellationToken ct)
        {
            var soma = 0m;

            foreach (var movimento in conta.Movimentacoes)
            {
                /*
                        Verifico se foi cancelado antes de cada iteração dentro do laço, pois se foi cancelado após entrar aqui nesse bloco,
                    ele vai ficar fazendo iterações/processos que nao são necessários
                    
                */

                ct.ThrowIfCancellationRequested();
                soma += movimento.Valor * FatorDeMultiplicacao(movimento.Data);
            }

            //Verifico também antes de um método que faz outro calculo
            ct.ThrowIfCancellationRequested();
            AtualizarInvestimentos(conta);

            return $"Cliente {conta.NomeCliente} tem saldo atualizado de R${soma.ToString("#00.00")}";
        }

        private static decimal FatorDeMultiplicacao(DateTime dataMovimento)
        {
            const decimal CTE_FATOR = 1.0000000005m;

            var diasCorridosDesdeDataMovimento = (dataMovimento - new DateTime(1900, 1, 1)).Days;
            var resultado = 1m;

            for (int i = 0; i < diasCorridosDesdeDataMovimento * 2; i++)
                resultado = resultado * CTE_FATOR;

            return resultado;
        }
        private static void AtualizarInvestimentos(ContaCliente cliente)
        {
            const decimal CTE_BONIFICACAO_MOV = 1m / (10m * 5m);
            cliente.Investimento *= CTE_BONIFICACAO_MOV;
        }
    }
}

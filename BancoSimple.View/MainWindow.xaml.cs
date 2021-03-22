using BancoSimple.Core.Model;
using BancoSimple.Core.Repository;
using BancoSimple.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BancoSimple.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        #region BtnProcessar_Click
        private void BtnProcessar_Click_Aula01(object sender, RoutedEventArgs e)
        {
            var contas = r_Repositorio.GetContaClientes();

            //Dividindo por 4 para pegar 4 nucleos do processador
            var contasQuantidadePorThread = contas.Count() / 4;

            var contas_parte1 = contas.Take(contasQuantidadePorThread);
            var contas_parte2 = contas.Skip(contasQuantidadePorThread).Take(contasQuantidadePorThread);
            var contas_parte3 = contas.Skip(contasQuantidadePorThread*2).Take(contasQuantidadePorThread);
            var contas_parte4 = contas.Skip(contasQuantidadePorThread*3);

            var resultado = new List<string>();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            //criando uma thread -- ela receve um delegate -- um lambda
            Thread thread_parte1 = new Thread(() =>
            {
                foreach (var conta in contas_parte1)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });
            Thread thread_parte2 = new Thread(() =>
            {
                foreach (var conta in contas_parte2)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });
            Thread thread_parte3 = new Thread(() =>
            {
                foreach (var conta in contas_parte3)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });
            Thread thread_parte4 = new Thread(() =>
            {
                foreach (var conta in contas_parte4)
                {
                    var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoProcessamento);
                }
            });

            //Inicializando a thread -- ela nao comeca enquanto nao é iniciada com esse metodo
            thread_parte1.Start();
            thread_parte2.Start();
            thread_parte3.Start();
            thread_parte4.Start();

            //Laço para fazer o codigo esperar todas as threads pararem
            while (thread_parte1.IsAlive || thread_parte2.IsAlive
                || thread_parte3.IsAlive || thread_parte4.IsAlive )
            {
                Thread.Sleep(250);
                //Não vou fazer nada
            }
            
            var fim = DateTime.Now;

            AtualizarView(resultado, fim - inicio);
        }
        
        private void BtnProcessar_Click_Aula02(object sender, RoutedEventArgs e)
        {
            var contas = r_Repositorio.GetContaClientes();

            //Desabilitando o botao para que o usuario nao clique varias vezes enquanto uma execução esta acontecendo
            BtnProcessar.IsEnabled = false;

            var resultado = new List<string>();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            /*
                Fazendo com que o .Net cuide das threads para mim

                    Posso fazer com que o gerenciamento das Threads disponíveis fique com TaskScheduler default 
                usado pela Task.Factory, que possui uma inteligência para saber quantos nucleos utilizar em cada caso.

            */

            var contasTarefa = contas.Select(conta => 
            {
                return Task.Factory.StartNew(() =>
                {
                    var resultaConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultaConta);
                }); 
            }).ToArray();

            /*
                    WaitAll() -- > Espera todas as tarefas acabarem
                  Porem para a thread principal que é responsável pela tela da aplicação, isso faz com que de a impressão de que a aplicação esteja
                travada
            
            */

            //Task.WaitAll(contasTarefa);

            //Pegar em qual thread a interface grafica esta rodando -- sera na thread principal
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();


            /*  
                      Para fazer com que a interface grafica nao pare, eu deixo uma tarefa intacta na thread principal (a tarefa da interface grafica),
                  enquanto as outras rodam 

                      No .Net se eu tento acessar um objeto da interface grafica atraves de uma thread diferente da principal, ele trava
                  por isso, eu passo para o continueWith a thread que eu quero que continue 
            */

            Task.WhenAll(contasTarefa)
                //Ele só ira executar a proxima tarefa quando a anterior acabar
                .ContinueWith(task =>
                {
                    var fim = DateTime.Now;
                    AtualizarView(resultado, fim - inicio);
                }, taskSchedulerUI)
                
                //Quero que ele continue disponivel apos atualizar a tela
                .ContinueWith(task =>
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);
        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            //Desabilitando o botao para que o usuario nao clique varias vezes enquanto uma execução esta acontecendo
            BtnProcessar.IsEnabled = false;

            _cts = new CancellationTokenSource();

            var contas = r_Repositorio.GetContaClientes();

            PgsProgresso.Maximum = contas.Count();

            limparView();

            var inicio = DateTime.Now;


            BtnCancelar.IsEnabled = true;
            
            // O .Net ja tam uma implementação para o progress -- que recebe uma action
            //var bancoSimpleProgress = new BancoSimpleProgress<string>(str =>
            //    PgsProgresso.Value++);

            var progress = new Progress<string>(str =>
                PgsProgresso.Value++);

            try
            {
                var resultado = await ConsolidarContas(contas, progress, _cts.Token);

                var fim = DateTime.Now;

                AtualizarView(resultado.ToList(), fim - inicio);
            }
            catch(OperationCanceledException)
            {
                TxtTempo.Text = "Operação cancelada pelo usuário";
            }
            finally
            {
                //o bloco finally é executado caindo no try ou no catch -- pois se caisse no bloco catch, ele não executaria as de mais linhas 

                BtnProcessar.IsEnabled = true;
                BtnCancelar.IsEnabled = false;
            }            

        }

        #endregion

        #region BtnCancelar_Click
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;

            //Invocando o método de cancelamento 
            _cts.Cancel();

            //todos os locais em que existe -> ct.IsCancellationRequested -- vão ser acionados retornando true
        }

        #endregion


        #region consolidarContas

        /*
            IEnumerable é a interface que os Lists implementam, por isso recebo ele  

            Task<List<String>> --> Estou retornando uma task que é uma list de string 

        */
        private async Task<string[]> ConsolidarContas_Aula04(IEnumerable<ContaCliente> contas)
        {
            //Explicitando novamente a thread principal para trabalhar com a GUI -- Interface grafica
            var taskSchedulerGui = TaskScheduler.FromCurrentSynchronizationContext();

            var tarefas = contas.Select(conta =>
              Task.Factory.StartNew(() =>
              {
                    var resultadoCondolidacao = r_Servico.ConsolidarMovimentacao(conta);

                    // Não podemos atualizar o ProgressBar dentro estando em uma thread que nao e a principal
                    // PgsProgresso.Value++;

                    Task.Factory.StartNew(
                        () => PgsProgresso.Value++,   //Passando como parametro a atualizacao da progressbar
                        CancellationToken.None, //Estou dizendo que nao quero passar nada para o parametro do tipo CancellationToken
                        TaskCreationOptions.None, //Estou dizendo que nao quero passar nada para o parametro do tipo TaskCreationOptions
                        taskSchedulerGui //passando como parametro a thread principal
                   );

                    return resultadoCondolidacao;
              })
            );
        
             return await Task.WhenAll(tarefas);   
        }

        private async Task<string[]> ConsolidarContas_Aula05(IEnumerable<ContaCliente> contas, IProgress<string> reportadorDeProgresso)
        {        
            var tarefas = contas.Select(conta =>
              Task.Factory.StartNew(() =>
              {
                  var resultadoCondolidacao = r_Servico.ConsolidarMovimentacao(conta);

                  /*
                        Como implementa uma interface eu posso criar um atributo do tipo da interface recebendo um objeto da classe especializada
                  */

                  reportadorDeProgresso.Report(resultadoCondolidacao);
         
                  return resultadoCondolidacao;
              })
            );

            return await Task.WhenAll(tarefas);
        }

        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas, IProgress<string> reportadorDeProgresso, CancellationToken ct )
        {
            var tarefas = contas.Select(conta =>
              Task.Factory.StartNew(() =>
              {
                  /*
                        Para cancelar a operação eu lanço uma exception antes de executar o processo que eu quero e antes do retorno do resultado
                  */

                  //Verifica se recebeu a notificação de que houve um cancelamento
                  /* 
                        if (ct.IsCancellationRequested)
                          throw new OperationCanceledException(ct);

                        Esse pequeno bloco pode virar:
                  */

                  ct.ThrowIfCancellationRequested();

                  var resultadoCondolidacao = r_Servico.ConsolidarMovimentacao(conta, ct);
          
                  reportadorDeProgresso.Report(resultadoCondolidacao);
                  
                  ct.ThrowIfCancellationRequested();

                  return resultadoCondolidacao;
              }, ct)
            );

            return await Task.WhenAll(tarefas);
        }

        #endregion


        #region ManipulaView
        private void limparView()
        {
            LstResultados.ItemsSource = null;

            TxtTempo.Text = null;

            PgsProgresso.Value = 0;
        }

        private void AtualizarView(List<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }

        #endregion


        #region Explicacao Async Await
        /*
                EXPLICAÇÃO DO PORQUE UTILIZAR OS NOMES ASYNC + AWAIT 
        */

        private void CodigoAntigo()
        {
           /*
                    Pode ser que existam codigos antigos utilizando await como variavel
                para que eles nao quebrem, await só se torna uma palavra reservada se tiver async na assinatura

           */

            var await = AlgumWifiAleatorioInjetandoTrojans();
            await.ToString();
        }

        private object AlgumWifiAleatorioInjetandoTrojans()
        {
            return null;
        }

        #endregion
    }

}

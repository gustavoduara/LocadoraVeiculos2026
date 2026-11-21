import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CONTATO_PROBLEMAS, CONTATO_TELEFONE } from './legal.constantes';
import { PaginaLegal } from './pagina-legal';

@Component({
    selector: 'app-termos-de-uso',
    imports: [PaginaLegal, RouterLink],
    template: `
        <app-pagina-legal titulo="Termos de Uso">
            <p>
                Estes Termos de Uso regulam o uso do <strong>SerraDrive</strong>. Ao criar uma conta, você declara que
                leu e aceita estes Termos e a
                <a class="text-primary underline" routerLink="/privacidade">Política de Privacidade</a>.
            </p>

            <h3>1. O que é o SerraDrive</h3>
            <p>
                O SerraDrive é uma plataforma com duas funções: (a) para <strong>locadoras de veículos</strong>, é uma
                ferramenta de gestão de frota, clientes, aluguéis, preços, cupons e reservas; (b) para
                <strong>clientes</strong>, é um catálogo onde se procuram carros de várias locadoras e se enviam pedidos
                de reserva.
            </p>
            <p>
                O SerraDrive <strong>não é uma locadora</strong> e não é parte do contrato de locação. O aluguel é
                combinado entre o cliente e a locadora escolhida.
            </p>

            <h3>2. Sua conta</h3>
            <ul>
                <li>Você precisa ter <strong>18 anos ou mais</strong> e, para alugar, uma CNH válida.</li>
                <li>Os dados informados devem ser verdadeiros e mantidos atualizados.</li>
                <li>É preciso <strong>confirmar o e-mail</strong> pelo link enviado para poder entrar.</li>
                <li>
                    Guarde a sua senha em segredo. Você responde pelo que for feito com a sua conta; avise-nos se achar
                    que alguém a usou sem permissão.
                </li>
            </ul>

            <h3>3. Para clientes</h3>
            <ul>
                <li>
                    A reserva é um <strong>pedido</strong>: ela só vale depois que a locadora a aprova. A locadora pode
                    recusar um pedido, e você pode cancelar enquanto ele estiver pendente.
                </li>
                <li>
                    Os valores mostrados no catálogo e na reserva são <strong>previsões</strong>. O valor final depende
                    da quilometragem rodada, do combustível, de taxas, de atrasos e das regras da locadora. A caução faz
                    parte do cálculo e é tratada entre você e a locadora.
                </li>
                <li>
                    A locadora pode exigir CNH válida durante todo o período do aluguel e outros documentos no momento
                    da retirada.
                </li>
                <li>
                    Ao enviar um pedido, você autoriza que seus dados de identificação, contato e CNH sejam enviados à
                    locadora escolhida, para que ela analise o pedido.
                </li>
            </ul>

            <h3>4. Para locadoras</h3>
            <ul>
                <li>
                    Você responde pelas informações que cadastra (frota, preços, planos, taxas, cupons) e pelo
                    cumprimento do contrato de locação com os seus clientes.
                </li>
                <li>
                    Em relação aos dados dos <strong>seus clientes e condutores</strong>, a locadora é a responsável
                    (controladora) perante a LGPD, e deve usá-los apenas para a operação dos aluguéis.
                </li>
                <li>
                    Contas de funcionários são criadas e desativadas pelo administrador da empresa, que responde pelo
                    uso que elas fizerem do sistema.
                </li>
            </ul>

            <h3>5. O que não é permitido</h3>
            <ul>
                <li>Usar dados de outras pessoas sem autorização ou criar contas falsas.</li>
                <li>Tentar acessar áreas, contas ou dados que não são seus, ou burlar limites e proteções do sistema.</li>
                <li>Enviar vírus, sobrecarregar o serviço de propósito ou fazer coleta automática em massa de dados.</li>
                <li>Usar o SerraDrive para qualquer fim ilegal.</li>
            </ul>
            <p>O descumprimento pode levar à suspensão ou ao encerramento da conta.</p>

            <h3>6. Propriedade intelectual</h3>
            <p>
                O nome, a marca, o visual e o código do SerraDrive pertencem aos seus autores. Os dados que você cadastra
                continuam sendo seus (ou da locadora, no caso de dados de clientes).
            </p>

            <h3>7. Disponibilidade e responsabilidade</h3>
            <p>
                O SerraDrive é um projeto em evolução e é oferecido <strong>"como está"</strong>. Podem ocorrer falhas,
                mudanças e interrupções para manutenção. Não garantimos que o serviço ficará sempre disponível nem livre de
                erros. Na medida permitida pela lei, não nos responsabilizamos por prejuízos decorrentes de contratos entre
                clientes e locadoras, de informações incorretas cadastradas por locadoras ou do uso indevido de contas por
                terceiros por descuido do usuário.
            </p>

            <h3>8. Encerramento</h3>
            <p>
                Você pode pedir a exclusão da sua conta a qualquer momento (veja a Política de Privacidade). Podemos
                suspender ou encerrar contas que violem estes Termos ou a lei.
            </p>

            <h3>9. Alterações</h3>
            <p>
                Estes Termos podem mudar. A versão em vigor, com a sua data, fica sempre nesta página. Mudanças
                relevantes serão avisadas no sistema ou por e-mail.
            </p>

            <h3>10. Lei aplicável e contato</h3>
            <p>
                Estes Termos seguem a legislação brasileira. Dúvidas, pedidos ou problemas: telefone e WhatsApp
                {{ telefone }} ou pelas
                <a class="text-primary underline" [href]="problemas" target="_blank" rel="noopener noreferrer">issues do projeto</a>.
            </p>
        </app-pagina-legal>
    `,
})
export class TermosDeUso {
    protected readonly telefone = CONTATO_TELEFONE;
    protected readonly problemas = CONTATO_PROBLEMAS;
}

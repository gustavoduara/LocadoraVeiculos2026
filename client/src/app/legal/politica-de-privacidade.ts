import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CONTATO_PROBLEMAS, CONTATO_TELEFONE } from './legal.constantes';
import { PaginaLegal } from './pagina-legal';

@Component({
    selector: 'app-politica-de-privacidade',
    imports: [PaginaLegal, RouterLink],
    template: `
        <app-pagina-legal titulo="Política de Privacidade">
            <p>
                Esta Política explica quais dados pessoais o <strong>SerraDrive</strong> trata, para quê, com quem são
                compartilhados e quais são os seus direitos, conforme a Lei Geral de Proteção de Dados (LGPD, Lei
                13.709/2018). Ela faz parte dos
                <a class="text-primary underline" routerLink="/termos-de-uso">Termos de Uso</a>.
            </p>

            <h3>1. Quem trata os seus dados</h3>
            <p>Há dois papéis, conforme o dado:</p>
            <ul>
                <li>
                    <strong>SerraDrive</strong> (projeto desenvolvido e mantido por Gustavo Duara): é o responsável pelos
                    dados da sua <strong>conta</strong> (nome, e-mail e senha) e pelos dados que você preenche em "Meus
                    dados" para pedir reservas.
                </li>
                <li>
                    <strong>A locadora</strong>: é a responsável pelos dados dos clientes e condutores que ela registra ao
                    fazer um aluguel, e pelos dados que recebe de você quando analisa um pedido de reserva. Nesses casos o
                    SerraDrive atua como operador, guardando os dados em nome da locadora.
                </li>
            </ul>

            <h3>2. Quais dados tratamos</h3>
            <ul>
                <li><strong>Conta:</strong> nome, e-mail e senha (a senha é guardada de forma irreversível, nunca em texto).</li>
                <li>
                    <strong>Cliente ("Meus dados" e reservas):</strong> CPF, RG, CNH e validade, telefone, endereço, e os
                    dados do pedido (veículo, período, plano, seguro e valores).
                </li>
                <li>
                    <strong>Locadora:</strong> dados da empresa e de funcionários (nome, CPF, e-mail, salário e data de
                    admissão), clientes, condutores, aluguéis, frota, preços e cupons.
                </li>
                <li>
                    <strong>Técnicos:</strong> o endereço IP, usado para limitar tentativas de login e cadastro, e
                    registros de funcionamento do servidor. O aceite destes Termos é guardado com a data e a versão.
                </li>
            </ul>

            <h3>3. Para que usamos (e a base legal)</h3>
            <ul>
                <li><strong>Criar e proteger a sua conta</strong> e confirmar o e-mail: execução do serviço que você pediu.</li>
                <li>
                    <strong>Enviar e analisar pedidos de reserva</strong> e registrar aluguéis: execução do serviço e do
                    contrato entre você e a locadora.
                </li>
                <li>
                    <strong>Enviar e-mails do serviço</strong>: confirmação de conta, redefinição de senha e resposta aos
                    seus pedidos de reserva.
                </li>
                <li>
                    <strong>Segurança</strong>: evitar fraudes, abusos e acessos indevidos (legítimo interesse).
                </li>
                <li>
                    <strong>Cumprir a lei</strong> e atender pedidos de autoridades, quando exigido.
                </li>
            </ul>
            <p>Não vendemos os seus dados nem os usamos para publicidade.</p>

            <h3>4. Com quem compartilhamos</h3>
            <ul>
                <li>
                    <strong>A locadora escolhida por você:</strong> ao enviar um pedido de reserva, ela recebe seus dados
                    de identificação, contato, CNH e endereço. O telefone da locadora só é mostrado a você depois da
                    aprovação.
                </li>
                <li>
                    <strong>Prestadores de serviço</strong> que fazem o sistema funcionar, como hospedagem do servidor e
                    do banco de dados e envio de e-mails. Eles só tratam os dados para prestar o serviço, e podem ficar em
                    servidores no Brasil ou no exterior.
                </li>
                <li><strong>Autoridades</strong>, quando houver obrigação legal.</li>
            </ul>
            <p>Uma locadora <strong>não enxerga</strong> os clientes, a frota nem os aluguéis de outra locadora.</p>

            <h3>5. Cookies e armazenamento no navegador</h3>
            <ul>
                <li>
                    <strong>Cookie de sessão (essencial):</strong> mantém você conectado e é protegido contra acesso por
                    scripts. Sem ele o login não funciona.
                </li>
                <li>
                    <strong>Preferências neste navegador:</strong> o modo noturno e a última escolha entre "Cliente" e
                    "Locadora" no login.
                </li>
            </ul>
            <p>Não usamos cookies de publicidade nem de rastreamento.</p>

            <h3>6. Por quanto tempo guardamos</h3>
            <p>
                Guardamos os dados enquanto a conta existir e pelo tempo necessário para cumprir obrigações legais, resolver
                disputas e manter o histórico dos aluguéis. Quando um registro comum é excluído no sistema, ele deixa de
                aparecer, mas pode permanecer no banco de dados por um período (exclusão lógica).
            </p>

            <h3>7. Excluir a sua conta</h3>
            <p>
                Você mesmo exclui a sua conta, a qualquer momento, em <strong>menu do seu nome &gt; Excluir minha conta</strong>
                (pedimos a senha e uma confirmação):
            </p>
            <ul>
                <li>
                    <strong>Cliente:</strong> a exclusão é <strong>imediata</strong>. Apagamos "Meus dados" (CPF, RG, CNH,
                    telefone e endereço), cancelamos os pedidos que esperavam resposta, encerramos as suas sessões e
                    anonimizamos a conta de login. O histórico que as locadoras guardam das suas reservas e aluguéis fica
                    <strong>sem os seus dados pessoais</strong> (nome e documentos viram "Conta excluída"), mantendo só datas,
                    veículo e valores, para a contabilidade da locadora. Se você tiver um aluguel em aberto, a exclusão só
                    é liberada depois de concluí-lo.
                </li>
                <li>
                    <strong>Locadora:</strong> o administrador agenda a exclusão para <strong>7 dias depois</strong> e pode
                    cancelar até lá. No dia, os dados da empresa são removidos, as contas dos funcionários são encerradas e os
                    dados pessoais de clientes, condutores e funcionários são anonimizados. O pedido é bloqueado se houver
                    aluguéis em aberto ou reservas esperando resposta.
                </li>
            </ul>
            <p>
                Se for um dado que uma locadora cadastrou por conta própria (por exemplo, um aluguel feito no balcão), o
                pedido também deve ser feito à própria locadora. Cópias de segurança do banco de dados podem levar algum
                tempo para ser renovadas.
            </p>

            <h3>8. Segurança</h3>
            <p>
                Usamos senhas guardadas de forma irreversível, confirmação de e-mail, limites de tentativas, sessões que
                podem ser revogadas e separação dos dados de cada locadora. Nenhum sistema é totalmente imune a falhas; se
                ocorrer um incidente que possa trazer risco a você, avisaremos como a lei exige.
            </p>

            <h3>9. Seus direitos</h3>
            <p>Você pode pedir, a qualquer momento:</p>
            <ul>
                <li>confirmação de que tratamos seus dados e acesso a eles;</li>
                <li>correção de dados incompletos ou desatualizados (você mesmo pode editar "Meus dados");</li>
                <li>anonimização, bloqueio ou eliminação de dados desnecessários ou tratados em desacordo com a lei (a exclusão da conta é feita por você mesmo, como na seção 7);</li>
                <li>portabilidade dos dados;</li>
                <li>informação sobre com quem compartilhamos os dados;</li>
                <li>revogação do consentimento, quando o tratamento se basear nele.</li>
            </ul>
            <p>
                Para dados de clientes registrados por uma locadora, o pedido pode precisar ser feito também à própria
                locadora, que é a responsável por eles.
            </p>

            <h3>10. Como falar com a gente</h3>
            <p>
                Para exercer seus direitos que não podem ser feitos pelo próprio sistema, ou para tirar dúvidas: telefone e WhatsApp {{ telefone }} ou pelas
                <a class="text-primary underline" [href]="problemas" target="_blank" rel="noopener noreferrer">issues do projeto</a>.
                Para sua segurança, podemos pedir uma confirmação de identidade antes de atender.
            </p>

            <h3>11. Menores de idade</h3>
            <p>O serviço é destinado a maiores de 18 anos. Não coletamos de propósito dados de menores.</p>

            <h3>12. Mudanças nesta Política</h3>
            <p>
                Podemos atualizar esta Política. A versão em vigor, com a sua data, fica sempre nesta página, e mudanças
                relevantes serão avisadas no sistema ou por e-mail.
            </p>
        </app-pagina-legal>
    `,
})
export class PoliticaDePrivacidade {
    protected readonly telefone = CONTATO_TELEFONE;
    protected readonly problemas = CONTATO_PROBLEMAS;
}

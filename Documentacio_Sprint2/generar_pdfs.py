# Genera els dos PDF individuals de l'Sprint 2 (Ioan Constantin i Pau Badosa).
# Execucio: python generar_pdfs.py
import os
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Image, Table, TableStyle,
                                PageBreak, KeepTogether)

AQUI = os.path.dirname(os.path.abspath(__file__))
CAP = os.path.join(AQUI, "captures")

VERD = colors.HexColor("#2E7D32")
VERD_CLAR = colors.HexColor("#E8F5E9")
GRIS = colors.HexColor("#555555")

h0 = ParagraphStyle("h0", fontName="Helvetica-Bold", fontSize=22, leading=26, textColor=VERD, spaceAfter=4)
sub = ParagraphStyle("sub", fontName="Helvetica", fontSize=11, leading=14, textColor=GRIS, spaceAfter=10)
h1 = ParagraphStyle("h1", fontName="Helvetica-Bold", fontSize=14, leading=18, textColor=VERD, spaceBefore=12, spaceAfter=6)
h2 = ParagraphStyle("h2", fontName="Helvetica-Bold", fontSize=11, leading=14, spaceBefore=6, spaceAfter=3)
body = ParagraphStyle("body", fontName="Helvetica", fontSize=10, leading=14, spaceAfter=4)
bul = ParagraphStyle("bul", parent=body, leftIndent=14, bulletIndent=4, spaceAfter=2)
cel = ParagraphStyle("cel", fontName="Helvetica", fontSize=8.5, leading=11)
celb = ParagraphStyle("celb", parent=cel, fontName="Helvetica-Bold", textColor=colors.white)
cap = ParagraphStyle("cap", fontName="Helvetica-Oblique", fontSize=8.5, leading=11, textColor=GRIS, spaceAfter=8)


def P(t, s=body):
    return Paragraph(t, s)


def punts(llista):
    return [Paragraph(t, bul, bulletText="•") for t in llista]


def taula(files, amples, cap_fila=True):
    dades = [[Paragraph(c, celb if (cap_fila and i == 0) else cel) for c in f] for i, f in enumerate(files)]
    t = Table(dades, colWidths=amples, repeatRows=1 if cap_fila else 0)
    est = [("VALIGN", (0, 0), (-1, -1), "TOP"),
           ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#BBBBBB")),
           ("TOPPADDING", (0, 0), (-1, -1), 3), ("BOTTOMPADDING", (0, 0), (-1, -1), 3)]
    if cap_fila:
        est += [("BACKGROUND", (0, 0), (-1, 0), VERD)]
        est += [("BACKGROUND", (0, i), (-1, i), VERD_CLAR) for i in range(2, len(files), 2)]
    t.setStyle(TableStyle(est))
    return t


def captura(fitxer, peu, ample=15.5 * cm):
    cami = os.path.join(CAP, fitxer)
    img = Image(cami)
    r = img.imageHeight / float(img.imageWidth)
    img.drawWidth = ample
    img.drawHeight = ample * r
    return KeepTogether([img, Spacer(1, 2), P(peu, cap)])


def peu_pagina(canvas, doc):
    canvas.saveState()
    canvas.setFont("Helvetica", 8)
    canvas.setFillColor(GRIS)
    canvas.drawString(2 * cm, 1.2 * cm, "BigBOOM 3D — Sprint 2 — " + doc.alumne)
    canvas.drawRightString(A4[0] - 2 * cm, 1.2 * cm, "Pag. %d" % doc.page)
    canvas.restoreState()


def construir(nom_fitxer, alumne, contingut):
    doc = SimpleDocTemplate(os.path.join(AQUI, nom_fitxer), pagesize=A4, leftMargin=2 * cm, rightMargin=2 * cm,
                            topMargin=1.8 * cm, bottomMargin=2 * cm, title="BigBOOM 3D - Sprint 2 - " + alumne, author=alumne)
    doc.alumne = alumne
    doc.build(contingut, onFirstPage=peu_pagina, onLaterPages=peu_pagina)
    print("Generat", nom_fitxer)


W = A4[0] - 4 * cm

# Comu a tots dos
CAPCALERA_COMU = [
    ["Projecte", "BigBOOM 3D (Buscamines 3D PvP per torns), Unity 6.3 LTS (6000.3.24f1), URP"],
    ["Equip", "Ioan Constantin (GitHub: Ioan-Cons / iconstantin-io) i Pau Badosa (GitHub: Pau_Badosa)"],
    ["Repositori", "GitHub iconstantin-io/BigBOOM — branques main, develop, feature/map-design, feature/sprint2-player"],
]


def capcalera(alumne, rol):
    return [P("Sprint 2 — Lliurament individual", h0),
            P("%s &nbsp;|&nbsp; %s" % (alumne, rol), sub),
            taula([["", ""]] + CAPCALERA_COMU, [3 * cm, W - 3 * cm], cap_fila=False)]


# ======================================================================
# IOAN
# ======================================================================
ioan = capcalera("Ioan Constantin", "Jugador, animacions i pantalles de control")
ioan[2] = taula(CAPCALERA_COMU, [3 * cm, W - 3 * cm], cap_fila=False)
ioan += [
    P("1. Tasques i responsabilitats", h1),
    P("Segons el repartiment de l'equip, les meves responsabilitats a l'Sprint 2 han estat:"),
    taula([
        ["Bloc de l'enunciat", "Tasca", "Estat"],
        ["1. Mecaniques i animacions", "Moviment i accions del jugador (caminar, girar, saltar)", "Completada"],
        ["1. Mecaniques i animacions", "Animacions i interaccions del personatge (Idle, Walking, Jumping amb Animator)", "Completada"],
        ["3. Pantalles i estats", "Pantalla d'inici (menu principal)", "Completada (versio base meva; l'estetica final la va treballar en Pau)"],
        ["3. Pantalles i estats", "Pantalla de pausa (ESC)", "Completada (versio base meva; ampliada despres amb controls i sliders)"],
        ["3. Pantalles i estats", "Pantalla de configuracio (volum)", "Completada (volum); la resta d'opcions les va afegir en Pau"],
    ], [4.2 * cm, 8 * cm, W - 12.2 * cm]),
    P("Completades", h2),
    *punts([
        "<b>PlayerController</b>: moviment amb WASD / fletxes, gir, salt i gravetat sobre un <i>CharacterController</i>. Suporta Input System i l'Input clasic, i ignora el teclat quan el joc esta en pausa.",
        "<b>Animator ac_player</b>: maquina d'estats Idle ↔ Walking ↔ Jumping controlada pels parametres <i>isWalking</i> i <i>isJump</i>, amb animacions de Mixamo (X Bot).",
        "<b>Personatges</b> afegits al projecte i configuracio del CharacterController (alcada, radi i centre).",
        "<b>Menu principal</b> (escena MainMenu) amb boto de jugar, seleccio de personatge, configuracio i sortir; <b>menu de pausa</b> amb Continuar / Sortir i control de volum.",
    ]),
    P("Pendents", h2),
    *punts([
        "Mes animacions (accions en picar una casella, celebracio de victoria / animacio de derrota).",
        "Que la seleccio de personatge del menu (<i>personajeSeleccionado</i>) canvii realment el model dins la partida.",
        "Pantalla de credits amb contingut.",
    ]),
    P("Col·laboracio amb en Pau", h1),
    P("El moviment del jugador i el sistema de joc (torns, tauler, HUD) s'havien d'enganxar entre ells. En Pau va afegir al meu "
      "PlayerController els ganxos que necessitava la logica de joc (<i>Teletransportar</i>, <i>MirarCap</i>, <i>Aturar</i>, "
      "moviment relatiu a la camera en 3a persona) i va refer l'estil visual dels menus que jo havia creat, afegint-hi la seleccio de "
      "mode (2 jugadors / bot / online) i de nivell. El moviment, l'Animator i l'estructura inicial dels menus son meus."),
    PageBreak(),
    P("2. Evidencies del treball realitzat", h1),
    P("Totes les captures s'han fet des de l'editor de Unity executant el joc (mode Play), partint del menu principal."),
    captura("joc_jugador_3a_persona.jpg", "Figura 1. El personatge amb l'animacio Idle sobre el tauler, vist en 3a persona (PlayerController + Animator ac_player)."),
    captura("menu_principal_nom.jpg", "Figura 2. Pantalla d'inici (menu principal) de BigBOOM 3D."),
    captura("menu_opcions.jpg", "Figura 3. Pantalla de configuracio: volum i sensibilitat del ratoli."),
    captura("joc_pausa.jpg", "Figura 4. Menu de pausa (tecla ESC): continuar, reiniciar, menu principal, sortir; amb la llegenda de controls."),
    PageBreak(),
    P("3. Evidencies de GitHub", h1),
    P("Commits i fusions meves relacionats amb l'Sprint 2 (branca <i>feature/sprint2-player</i>, integrada a <i>develop</i> amb el Pull Request #3):"),
    taula([
        ["Hash", "Data", "Missatge", "Tasca"],
        ["62edde1", "28-10-2026", "Afegits personatges, animacions de Mixamo, configuracio del CharacterController i correccio del salt al PlayerController", "Moviment + animacions"],
        ["b2db0f9", "29-10-2026", "Afegir Menu Principal i Menu Pausa", "Pantalles d'inici / pausa / configuracio"],
        ["27e1944", "29-10-2026", "Afegir Scenes", "Escenes del projecte"],
        ["063134f", "29-10-2026", "Merge pull request #3 (feature/sprint2-player)", "Integracio a develop"],
    ], [1.8 * cm, 2.2 * cm, W - 8.2 * cm, 4.2 * cm]),
    Spacer(1, 8),
    P("Resum de l'historial: el commit 62edde1 afegeix 3 animacions FBX (Idle, Walking, Jumping), el controlador <i>ac_player.controller</i> "
      "i el PlayerController; el commit b2db0f9 afegeix <i>MainMenuController.cs</i>, <i>PauseMenuController.cs</i> i l'escena <i>MainMenu.unity</i>."),
    P("<i>[Afegiu aqui captures de GitHub: llista de commits de la branca feature/sprint2-player i el Pull Request #3.]</i>", cap),
    P("4. Dificultats i solucions", h1),
    *punts([
        "<b>El salt no funcionava be:</b> el personatge es quedava en l'animacio de salt o no saltava. Es va solucionar comprovant <i>isGrounded</i> abans de moure i fent un unic <i>controller.Move</i> amb horitzontal + vertical.",
        "<b>Animacions de Mixamo:</b> en importar-les calia configurar el tipus de rig (Humanoid) i activar el bucle perque Idle i Walking no s'aturessin.",
        "<b>Integracio amb el joc:</b> el moviment s'havia d'aturar quan hi havia el menu de pausa; es llegeix <i>PauseMenuController.EstaPausat</i> i s'anul·la l'entrada.",
    ]),
]

# ======================================================================
# PAU
# ======================================================================
pau = capcalera("Pau Badosa", "Mecaniques de joc, HUD i estats de partida")
pau[2] = taula(CAPCALERA_COMU, [3 * cm, W - 3 * cm], cap_fila=False)
pau += [
    P("1. Tasques i responsabilitats", h1),
    P("Segons el repartiment de l'equip, les meves responsabilitats a l'Sprint 2 han estat:"),
    taula([
        ["Bloc de l'enunciat", "Tasca", "Estat"],
        ["1. Mecaniques i animacions", "Obstacles / elements que afecten el jugador: mines del tauler, explosions", "Completada"],
        ["1. Mecaniques i animacions", "Sistema de puntuacio, torns i objectius (segons el GDD)", "Completada"],
        ["2. HUD i interficie", "HUD de partida: marcador, temps, torn, punts del torn, mines / banderes", "Completada"],
        ["3. Pantalles i estats", "Final de partida (guanya / empat) i resum de cada torn", "Completada"],
        ["Extra", "Mode contra el bot, mode online, seleccio de nivell", "Completada (primera versio)"],
    ], [4.2 * cm, 8 * cm, W - 12.2 * cm]),
    P("Completades", h2),
    *punts([
        "<b>JocBuscamines</b>: logica completa del buscamines per torns: genera el mapa amb llavor, zona inicial oberta en cascada, destapar caselles, posar / treure banderes, 10 torns de 15 s, vista aeria i compte enrere.",
        "<b>Puntuacio (GDD)</b>: mina -5, bandera correcta +1, bandera erronia -1. Al final del torn es compten les banderes i es mostren totes les mines.",
        "<b>Mines com a obstacle</b>: en destapar una mina el torn acaba, es mostra una explosio (runes, fum) i la camera trepitja (<i>EfectesJoc</i>, <i>CameraJoc</i>).",
        "<b>GeneradorTaulell</b> i prefabs de caselles (tapades / descobertes, 2 models alternats, numeros 0-8 i mina); <b>GeneradorIlla</b> per a l'illa del tauler.",
        "<b>HUD</b> (<i>GeneradorHUD</i> a l'espai 3D i <i>HudJugador</i> a pantalla): targetes J1 / J2 amb punts, cercle de temps, torn actual, punts del torn, comptador de mines i banderes.",
        "<b>Final de partida</b>: text GUANYA &lt;jugador&gt; / EMPAT i boto de tornar a jugar.",
        "<b>Rival bot</b> (<i>BotBuscamines</i>): dedueix caselles segures i mines nomes amb el que veu un jugador; si no pot, s'arrisca. <b>Online</b> basic per xarxa (<i>XarxaBigBOOM</i>, <i>ConfigPartida</i>).",
    ]),
    P("Pendents", h2),
    *punts([
        "So i musica (efectes d'explosio, clic, banderes).",
        "Equilibrar el bot i les puntuacions amb proves de joc amb mes gent; provar l'online en dues maquines reals.",
        "Capturar i documentar la pantalla final de victoria / derrota (la logica i el panell estan fets; falta la captura).",
        "Netejar els avisos de la consola (p. ex. el caracter ★ que no existeix a la font del HUD).",
    ]),
    P("Col·laboracio amb en Ioan", h1),
    P("El jugador i l'Animator els ha fet en Ioan; jo he usat el seu <i>PlayerController</i> afegint-hi els ganxos que necessita el joc "
      "(teletransport a la zona inicial, mirar cap a la casella picada, aturar el personatge quan no li toca). El menu principal i la pausa "
      "els va crear ell; jo hi he aplicat un estil visual comu amb el HUD i hi he afegit la seleccio de mode i de nivell, el nom del jugador "
      "i les opcions de sensibilitat."),
    PageBreak(),
    P("2. Evidencies del treball realitzat", h1),
    P("Captures fetes des de l'editor de Unity en mode Play, mode <i>Contra el bot</i>, nivell Principiant (9x9, 10 mines)."),
    captura("joc_hud_torn.jpg", "Figura 1. HUD de partida: targetes dels jugadors amb punts, temps del torn (cercle), torn actual i comptador de mines / banderes."),
    captura("joc_fi_torn_mines.jpg", "Figura 2. Final de torn: es destapen totes les mines i es mostra el resum (punts, banderes bones i erronies)."),
    captura("joc_explosio_bot.jpg", "Figura 3. Una mina que explota (torn del bot): runes, fum, sacseig de camera i -3 punts."),
    PageBreak(),
    captura("menu_mode.jpg", "Figura 4. Seleccio de mode: 2 jugadors, contra el bot o online."),
    captura("menu_nivell.jpg", "Figura 5. Seleccio de nivell (9x9 amb 10 mines, 16x16 amb 40, 30x16 amb 99)."),
    captura("editor_escena.jpg", "Figura 6. Escena EscenaMapa a l'editor: Taulell, Joc (logica), HUD i illa."),
    PageBreak(),
    P("3. Evidencies de GitHub", h1),
    P("Commits meus relacionats amb l'Sprint 2:"),
    taula([
        ["Hash", "Data", "Missatge", "Tasca"],
        ["b37776d", "27-10-2026", "Mapa, Taulell, Hud_Partida", "Prefabs de caselles, taulell i HUD"],
        ["861f7f9", "27-10-2026", "Scripts_Afagir", "Carpeta de scripts"],
        ["4fc39d7", "27-10-2026", "Scripts_update", "GeneradorTaulell, GeneradorHUD, GeneradorIlla, JocBuscamines"],
        ["00947a7", "27-10-2026", "Afegir materials", "Materials de caselles i taulell"],
        ["363a260", "29-10-2026", "V_PRE_CANVIS: abans dels 7 canvis (menu, nivells, nom, vista aeria, HUD)", "Efectes, camera, HUD de jugador"],
        ["06ee488", "29-10-2026", "V_PRE_ONLINE: abans d'afegir el multijugador", "Punt de control"],
        ["47886c1", "30-10-2026", "commit_buscaminesComplet", "Bot, online, puntuacio i final de partida"],
    ], [1.8 * cm, 2.2 * cm, W - 8.6 * cm, 4.6 * cm]),
    Spacer(1, 8),
    P("Els scripts de la carpeta <i>Assets/Scripts (Pau)</i> i <i>Assets/Scripts/Joc</i> sumen unes 2.600 linies de C# que he escrit jo."),
    P("<i>[Afegiu aqui captures de GitHub: historial de commits de la branca develop i del meu usuari Pau_Badosa.]</i>", cap),
    P("4. Dificultats i solucions", h1),
    *punts([
        "<b>Prefabs en rosa:</b> les caselles es veien de color rosa perque els materials feien servir el shader <i>Standard</i> (built-in) i el projecte fa servir URP. Es van passar els materials a <i>Universal Render Pipeline/Lit</i> mantenint el color base.",
        "<b>La mina delatava la seva posicio:</b> les caselles tapades amb mina porten la mina a dins, desplacada. Per calcular el centre d'una casella es fan servir els <i>colliders</i> i no el model.",
        "<b>La decoracio tapava el tauler:</b> arbres i pedres de l'escena creixien sobre els nivells grans. S'hi va afegir una neteja automatica que desactiva el que toca el tauler.",
        "<b>Sincronitzar en online:</b> es va decidir que el host decideix la llavor del mapa i les fases, i el client rep els missatges, perque tots dos generin exactament el mateix tauler.",
        "<b>Controls amb 3a persona i ratoli:</b> es va afegir una mira i es va fer que el personatge miri la casella picada per donar feedback.",
    ]),
]

if __name__ == "__main__":
    construir("Sprint2_Ioan_Constantin.pdf", "Ioan Constantin", ioan)
    construir("Sprint2_Pau_Badosa.pdf", "Pau Badosa", pau)

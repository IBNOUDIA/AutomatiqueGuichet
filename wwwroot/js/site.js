// Ajoute un chiffre au champ ciblé depuis un pavé numérique à l'écran (style guichet).
function ajouterChiffre(idChamp, chiffre, longueurMax) {
    const champ = document.getElementById(idChamp);
    if (!champ) return;
    if (champ.value.length < longueurMax) {
        champ.value += chiffre;
    }
    champ.focus();
}

function effacerChamp(idChamp) {
    const champ = document.getElementById(idChamp);
    if (!champ) return;
    champ.value = "";
    champ.focus();
}

function effacerDernierChiffre(idChamp) {
    const champ = document.getElementById(idChamp);
    if (!champ) return;
    champ.value = champ.value.slice(0, -1);
    champ.focus();
}

// Filtre en direct la liste des clients (panneau admin) par nom, prénom ou code client.
function filtrerClients(recherche) {
    const corps = document.getElementById("corpsListeClients");
    const messageAucunResultat = document.getElementById("messageAucunResultat");
    if (!corps) return;

    const termeRecherche = recherche.trim().toLowerCase();
    let resultatsTrouves = 0;

    for (const ligne of corps.rows) {
        const correspond = (ligne.dataset.recherche || "").includes(termeRecherche);
        ligne.hidden = !correspond;
        if (correspond) resultatsTrouves++;
    }

    if (messageAucunResultat) {
        messageAucunResultat.hidden = resultatsTrouves > 0;
    }
}

// Panneau admin : ouvre l'onglet Historique quand on filtre par compte
// (lien « Historique » de la liste des comptes ou formulaire de filtre).
document.addEventListener("DOMContentLoaded", () => {
    const filtreCompte = new URLSearchParams(window.location.search).get("CompteConsulte");
    if (window.location.hash !== "#historique" && !filtreCompte) return;

    const bouton = document.querySelector('#adminTabs [data-bs-target="#historique"]');
    if (bouton && window.bootstrap) bootstrap.Tab.getOrCreateInstance(bouton).show();
});

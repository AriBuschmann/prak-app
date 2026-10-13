// jQWidgets erwartet jQuery als globale Variable. Dieses Modul muss deshalb
// VOR den jqx*-Skripten importiert werden (siehe ./index.ts).
import $ from 'jquery';

const w = window as unknown as { $: JQueryStatic; jQuery: JQueryStatic };
w.$ = w.jQuery = $;

export default $;

import { useEffect, useMemo, useState } from "react";
import { api } from "../services/api";
import type { Lead, Quotation, LeadService } from "../services/api";
import "./QuotationsPage.css";

type QuoteLine = LeadService & { quantity:number; unit:string; unitPrice:number; taxPercent:number; discountAmount:number };

function money(value:number){ return value.toLocaleString("en-IN",{minimumFractionDigits:2,maximumFractionDigits:2}); }
function defaultValidUntil(){ const d=new Date(); d.setDate(d.getDate()+15); return d.toISOString().slice(0,10); }
function itemType(s:LeadService){ const c=(s.optionCode||"").toUpperCase(); if(c==="SUPPLY")return "SUPPLY"; if(c==="RESOURCE")return "RESOURCE"; if(c==="COORDINATION")return "COORDINATION"; return "OUR_SERVICE"; }

function QuotationsPage(){
 const [quotations,setQuotations]=useState<Quotation[]>([]),[qualifiedLeads,setQualifiedLeads]=useState<Lead[]>([]);
 const [selectedLeadId,setSelectedLeadId]=useState(""),[lines,setLines]=useState<QuoteLine[]>([]);
 const [quotationDate,setQuotationDate]=useState(new Date().toISOString().slice(0,10)),[validUntil,setValidUntil]=useState(defaultValidUntil());
 const [notes,setNotes]=useState(""),[saving,setSaving]=useState(false),[loading,setLoading]=useState(true),[showForm,setShowForm]=useState(false),[error,setError]=useState("");

 async function load(){try{setLoading(true);setError("");const [qs,ls]=await Promise.all([api.getQuotations(),api.getLeads()]);setQuotations(qs);const leads: Lead[]=ls;setQualifiedLeads(leads.filter(l=>String(l.status||"").trim().toUpperCase()==="QUALIFIED"));}catch(e){setError(e instanceof Error?e.message:"Unable to load quotations.");}finally{setLoading(false);}}
 useEffect(()=>{load();},[]);

 const selectedLead=qualifiedLeads.find(l=>l.leadId===Number(selectedLeadId));
 async function chooseLead(value:string){setSelectedLeadId(value);setLines([]);if(!value)return;try{const lead=await api.getLead(Number(value));setLines((lead.services||[]).map(s=>({...s,quantity:1,unit:"LS",unitPrice:0,taxPercent:0,discountAmount:0})));}catch(e){setError(e instanceof Error?e.message:"Unable to load lead services.");}}
 function updateLine(index:number,field:keyof QuoteLine,value:string){setLines(cur=>cur.map((line,i)=>{if(i!==index)return line;if(["quantity","unitPrice","taxPercent","discountAmount"].includes(field as string))return {...line,[field]:Number(value)||0};return {...line,[field]:value};}));}
 const totals=useMemo(()=>{let subTotal=0,discount=0,tax=0;for(const l of lines){const gross=l.quantity*l.unitPrice,taxable=Math.max(0,gross-l.discountAmount);subTotal+=gross;discount+=l.discountAmount;tax+=Math.round(taxable*l.taxPercent)/100;}return{subTotal,discount,tax,grandTotal:Math.max(0,subTotal-discount+tax)};},[lines]);
 function openNew(){setError("");setSelectedLeadId("");setLines([]);setQuotationDate(new Date().toISOString().slice(0,10));setValidUntil(defaultValidUntil());setNotes("");setShowForm(true);}
 async function createQuotation(){if(!selectedLeadId){setError("Please select a qualified lead.");return;}if(!lines.length){setError("The selected lead has no services.");return;}try{setSaving(true);setError("");await api.createQuotation({leadId:Number(selectedLeadId),quotationDate,validUntil:validUntil||null,notes:notes.trim()||null,createdByUserId:1,items:lines.map((l,i)=>({itemType:itemType(l),description:l.serviceName,quantity:l.quantity,unit:l.unit,unitPrice:l.unitPrice,discountAmount:l.discountAmount,taxPercent:l.taxPercent,notes:l.requirement||l.optionName||null,displayOrder:i+1}))});setShowForm(false);await load();}catch(e){setError(e instanceof Error?e.message:"Unable to create quotation.");}finally{setSaving(false);}}
 async function changeStatus(q:Quotation,status:string){try{setError("");await api.updateQuotationStatus(q.quotationId,status);await load();}catch(e){setError(e instanceof Error?e.message:"Unable to update quotation status.");}}
 function statusClass(status:string){return "quote-status quote-"+status.toLowerCase();}

 return <div className="quotation-page">
  <div className="module-header"><div><h2>Quotations</h2><p>Create, review and manage customer quotations from qualified leads.</p></div><button className="primary-button" onClick={openNew} disabled={!qualifiedLeads.length}>+ Create Quotation</button></div>
  <div className="quote-summary"><div><span>Total</span><strong>{quotations.length}</strong></div><div><span>Draft</span><strong>{quotations.filter(q=>q.status==="DRAFT").length}</strong></div><div><span>Customer Review</span><strong>{quotations.filter(q=>q.status==="CUSTOMER_REVIEW").length}</strong></div><div><span>Approved</span><strong>{quotations.filter(q=>q.status==="APPROVED").length}</strong></div></div>
  {error&&<div className="quote-error">{error}</div>}
  <div className="leads-panel">{loading?<div className="page-empty"><h3>Loading quotations...</h3></div>:!quotations.length?<div className="page-empty"><h3>No quotations yet</h3><p>Qualified leads can be converted into quotations.</p></div>:<div className="table-container"><table className="data-table"><thead><tr><th>Quotation</th><th>Customer</th><th>Date</th><th>Items</th><th>Total</th><th>Status</th><th>Action</th></tr></thead><tbody>{quotations.map(q=><tr key={q.quotationId}><td><strong>{q.quotationNumber}</strong><span className="quote-muted">{q.leadCode||"-"}</span></td><td>{q.customerName||"-"}</td><td>{q.quotationDate||"-"}</td><td>{q.itemCount??0}</td><td>₹ {money(q.grandTotal||0)}</td><td><span className={statusClass(q.status)}>{q.status.replaceAll("_"," ")}</span></td><td>{q.status==="DRAFT"&&<button className="site-view-button" onClick={()=>changeStatus(q,"SENT")}>Send</button>}{q.status==="SENT"&&<button className="site-view-button" onClick={()=>changeStatus(q,"CUSTOMER_REVIEW")}>Customer Review</button>}</td></tr>)}</tbody></table></div>}</div>
  {showForm&&<div className="site-overlay" onClick={()=>!saving&&setShowForm(false)}><aside className="site-drawer site-drawer-wide" onClick={e=>e.stopPropagation()}>
   <div className="site-drawer-header"><div><div className="site-drawer-kicker">QUOTATION</div><h2>Create Quotation</h2><p>Select a qualified lead and price the services requested by the customer.</p></div><button className="site-close-button" onClick={()=>!saving&&setShowForm(false)}>×</button></div>
   <div className="quote-form">
    <label><span>Qualified Lead</span><select value={selectedLeadId} onChange={e=>chooseLead(e.target.value)}><option value="">Select qualified lead</option>{qualifiedLeads.map(l=><option key={l.leadId} value={l.leadId}>{l.leadCode} — {l.customerName}{l.companyName?" — "+l.companyName:""}</option>)}</select></label>
    {selectedLead&&<div className="quote-customer-card"><strong>{selectedLead.customerName}</strong><span>{selectedLead.companyName||"-"}</span><span>{selectedLead.mobile||"-"}</span><span>{selectedLead.location||"-"}</span></div>}
    <div className="quote-date-grid"><label><span>Quotation Date</span><input type="date" value={quotationDate} onChange={e=>setQuotationDate(e.target.value)}/></label><label><span>Valid Until</span><input type="date" value={validUntil} onChange={e=>setValidUntil(e.target.value)}/></label></div>
    <div className="quote-items-header"><h3>Services & Pricing</h3><span>Enter quantity, unit and price for each selected service.</span></div>
    {!lines.length?<div className="quote-empty">Select a qualified lead to load its selected services.</div>:<div className="quote-lines">{lines.map((l,i)=>{const gross=l.quantity*l.unitPrice,taxable=Math.max(0,gross-l.discountAmount),lineTax=Math.round(taxable*l.taxPercent)/100;return <div className="quote-line" key={l.serviceId+"-"+i}><div className="quote-line-title"><strong>{l.serviceName}</strong><span>{l.optionName||l.optionCode||""}</span>{l.requirement&&<small>{l.requirement}</small>}</div><div className="quote-line-fields"><label><span>Qty</span><input type="number" min="0" step="0.01" value={l.quantity} onChange={e=>updateLine(i,"quantity",e.target.value)}/></label><label><span>Unit</span><input value={l.unit} onChange={e=>updateLine(i,"unit",e.target.value)}/></label><label><span>Rate</span><input type="number" min="0" step="0.01" value={l.unitPrice} onChange={e=>updateLine(i,"unitPrice",e.target.value)}/></label><label><span>Disc.</span><input type="number" min="0" step="0.01" value={l.discountAmount} onChange={e=>updateLine(i,"discountAmount",e.target.value)}/></label><label><span>Tax %</span><input type="number" min="0" step="0.01" value={l.taxPercent} onChange={e=>updateLine(i,"taxPercent",e.target.value)}/></label><div className="quote-line-total"><span>Total</span><strong>₹ {money(taxable+lineTax)}</strong></div></div></div>})}</div>}
    <label><span>Notes</span><textarea rows={4} value={notes} onChange={e=>setNotes(e.target.value)} placeholder="Quotation notes, assumptions, exclusions or commercial terms"/></label>
    <div className="quote-total-box"><div><span>Subtotal</span><strong>₹ {money(totals.subTotal)}</strong></div><div><span>Discount</span><strong>₹ {money(totals.discount)}</strong></div><div><span>Tax</span><strong>₹ {money(totals.tax)}</strong></div><div className="grand"><span>Grand Total</span><strong>₹ {money(totals.grandTotal)}</strong></div></div>
    <div className="site-form-actions"><button className="secondary-button" onClick={()=>setShowForm(false)} disabled={saving}>Cancel</button><button className="primary-button" onClick={createQuotation} disabled={saving||!selectedLeadId||!lines.length}>{saving?"Creating...":"Create Draft Quotation"}</button></div>
   </div>
  </aside></div>}
 </div>;
}
export default QuotationsPage;
